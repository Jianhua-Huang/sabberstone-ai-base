const fs = require("fs");
const path = require("path");
const { spawnSync } = require("child_process");

const repoRoot = path.resolve(__dirname, "..");
const analysisDir = __dirname;
const logPath = path.join(analysisDir, "prefetch-hsdata.log");
const hsdataRepo = "https://github.com/HearthSim/hsdata.git";
const rawBase = "https://raw.githubusercontent.com/HearthSim/hsdata";
const fallbackTags = [44222, 44582, 45310, 45932, 47374, 48313, 48705];

function argValue(name, fallback) {
	const prefix = `--${name}=`;
	const item = process.argv.find(p => p.startsWith(prefix));
	if (item) return item.slice(prefix.length);
	const index = process.argv.indexOf(`--${name}`);
	if (index >= 0 && process.argv[index + 1]) return process.argv[index + 1];
	return fallback;
}

function log(message) {
	const line = `[${new Date().toISOString()}] ${message}\n`;
	fs.appendFileSync(logPath, line);
	process.stdout.write(line);
}

function run(command, args, options = {}) {
	const result = spawnSync(command, args, {
		cwd: repoRoot,
		env: {
			...process.env,
			https_proxy: process.env.https_proxy || "http://127.0.0.1:33210",
			http_proxy: process.env.http_proxy || "http://127.0.0.1:33210",
			all_proxy: process.env.all_proxy || "socks5://127.0.0.1:33210"
		},
		encoding: "utf8",
		...options
	});
	if (result.status !== 0) {
		throw new Error(`${command} ${args.join(" ")} failed: ${result.stderr || result.stdout}`);
	}
	return result.stdout;
}

function remoteTagsAfter(fromBuild) {
	try {
		const output = run("git", ["ls-remote", "--tags", hsdataRepo], { timeout: 120000 });
		const tags = [];
		for (const line of output.split(/\r?\n/)) {
			const match = line.match(/refs\/tags\/(\d+)$/);
			if (match) tags.push(Number(match[1]));
		}
		return [...new Set(tags)].filter(t => t > fromBuild).sort((a, b) => a - b);
	} catch (error) {
		log(`remote tag list failed, using fallback tags: ${error.message}`);
		return fallbackTags.filter(t => t > fromBuild);
	}
}

function downloadCardDefs(build) {
	const target = path.join(analysisDir, `CardDefs-${build}.xml`);
	if (fs.existsSync(target) && fs.statSync(target).size > 1000000) {
		log(`CardDefs-${build}.xml exists, skip download`);
		return target;
	}

	const tmp = `${target}.download`;
	if (fs.existsSync(tmp)) fs.unlinkSync(tmp);
	const url = `${rawBase}/${build}/CardDefs.xml`;
	log(`downloading ${url}`);
	run("curl.exe", ["-L", "--retry", "8", "--retry-delay", "5", "--connect-timeout", "30", "-o", tmp, url], { timeout: 1800000 });
	const size = fs.statSync(tmp).size;
	if (size < 1000000) throw new Error(`downloaded CardDefs-${build}.xml is too small: ${size}`);
	fs.renameSync(tmp, target);
	log(`downloaded CardDefs-${build}.xml (${size} bytes)`);
	return target;
}

function parseCardDefs(file) {
	const text = fs.readFileSync(file, "utf8");
	const entities = new Map();
	const entityRegex = /<Entity CardID="([^"]+)" ID="([^"]+)"[\s\S]*?<\/Entity>/g;
	for (const entityMatch of text.matchAll(entityRegex)) {
		const id = entityMatch[1];
		const body = entityMatch[0];
		const tags = {};
		const tagRegex = /<Tag enumID="([^"]+)" name="([^"]+)" type="[^"]+"(?: value="([^"]*)")?[^>]*>([\s\S]*?)<\/Tag>|<Tag enumID="([^"]+)" name="([^"]+)" type="[^"]+" value="([^"]*)"\s*\/>/g;
		for (const tagMatch of body.matchAll(tagRegex)) {
			const name = tagMatch[2] || tagMatch[6];
			const value = tagMatch[3] ?? tagMatch[7] ?? (((tagMatch[4] || "").match(/<enUS>([\s\S]*?)<\/enUS>/) || [])[1]) ?? "";
			tags[name] = value;
		}
		entities.set(id, { id, tags, name: tags.CARDNAME || "" });
	}
	return entities;
}

function ignored(entity) {
	if (!entity) return false;
	const id = entity.id;
	const set = entity.tags.CARD_SET;
	return set === "1453" ||
		set === "18" ||
		set === "17" ||
		id.startsWith("BGS_") ||
		id.startsWith("TB_Bacon") ||
		id.startsWith("FB_Champs_");
}

function diffCardDefs(oldBuild, newBuild) {
	const oldFile = path.join(analysisDir, `CardDefs-${oldBuild}.xml`);
	const newFile = path.join(analysisDir, `CardDefs-${newBuild}.xml`);
	const oldCards = parseCardDefs(oldFile);
	const newCards = parseCardDefs(newFile);
	const added = [];
	const removed = [];
	const changed = [];

	for (const [id, entity] of newCards) {
		if (!oldCards.has(id) && !ignored(entity)) added.push(entity);
	}

	for (const [id, entity] of oldCards) {
		if (!newCards.has(id) && !ignored(entity)) removed.push(entity);
	}

	for (const [id, next] of newCards) {
		const prev = oldCards.get(id);
		if (!prev || ignored(prev) || ignored(next)) continue;
		const keys = new Set([...Object.keys(prev.tags), ...Object.keys(next.tags)]);
		for (const key of keys) {
			const oldValue = prev.tags[key] ?? "";
			const newValue = next.tags[key] ?? "";
			if (oldValue !== newValue) {
				changed.push({
					id,
					name: next.name,
					field: key,
					old: oldValue,
					new: newValue,
					collectible: next.tags.COLLECTIBLE === "1",
					set: next.tags.CARD_SET || ""
				});
			}
		}
	}

	const summary = {
		oldBuild,
		newBuild,
		oldCount: oldCards.size,
		newCount: newCards.size,
		addedRelevant: added.length,
		removedRelevant: removed.length,
		changedRelevantFields: changed.length,
		added,
		removed,
		changed
	};

	fs.writeFileSync(
		path.join(analysisDir, `diff-${oldBuild}-to-${newBuild}.json`),
		JSON.stringify(summary, null, 2)
	);

	const md = [
		`# Prefetch diff: hsdata ${oldBuild} -> ${newBuild}`,
		"",
		"Scope: traditional 1v1 only. Battlegrounds, Tavern Brawl, and hero skins are filtered out.",
		"",
		"## Summary",
		"",
		`- Old entity count: ${oldCards.size}`,
		`- New entity count: ${newCards.size}`,
		`- Added relevant entities: ${added.length}`,
		`- Removed relevant entities: ${removed.length}`,
		`- Changed relevant fields: ${changed.length}`,
		"",
		"## Added",
		"",
		...formatEntities(added),
		"",
		"## Removed",
		"",
		...formatEntities(removed),
		"",
		"## Changed",
		"",
		...formatChanges(changed)
	].join("\n");
	fs.writeFileSync(path.join(analysisDir, `prefetch-${oldBuild}-to-${newBuild}.md`), md);
	log(`wrote diff-${oldBuild}-to-${newBuild}.json and prefetch-${oldBuild}-to-${newBuild}.md`);
}

function formatEntities(items) {
	if (items.length === 0) return ["- None"];
	return items.map(e => `- ${e.id}: ${e.name || "(no name)"}; set=${e.tags.CARD_SET || ""}; type=${e.tags.CARDTYPE || ""}; collectible=${e.tags.COLLECTIBLE || "0"}`);
}

function formatChanges(items) {
	if (items.length === 0) return ["- None"];
	return items.map(c => `- ${c.id}: ${c.name || "(no name)"}; ${c.field}: ${singleLine(c.old)} -> ${singleLine(c.new)}; collectible=${c.collectible ? "1" : "0"}; set=${c.set}`);
}

function singleLine(value) {
	return String(value).replace(/\s+/g, " ").trim();
}

async function main() {
	const fromBuild = Number(argValue("from", "43246"));
	const limit = Number(argValue("limit", "0"));
	fs.writeFileSync(logPath, "");
	log(`prefetch started from ${fromBuild}`);
	let tags = remoteTagsAfter(fromBuild);
	if (limit > 0) tags = tags.slice(0, limit);
	log(`queued builds: ${tags.join(", ") || "(none)"}`);

	let previous = fromBuild;
	for (const build of tags) {
		try {
			downloadCardDefs(build);
			diffCardDefs(previous, build);
			previous = build;
		} catch (error) {
			log(`failed build ${build}: ${error.stack || error.message}`);
		}
	}
	log("prefetch finished");
}

main().catch(error => {
	log(`fatal: ${error.stack || error.message}`);
	process.exitCode = 1;
});
