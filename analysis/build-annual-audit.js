const fs = require("fs");
const path = require("path");

const analysisDir = __dirname;

function parseCardDefs(file) {
	const text = fs.readFileSync(file, "utf8");
	const entities = new Map();
	const entityRegex = /<Entity CardID="([^"]+)" ID="([^"]+)"[\s\S]*?<\/Entity>/g;
	for (const entityMatch of text.matchAll(entityRegex)) {
		const cardId = entityMatch[1];
		const entityId = entityMatch[2];
		const body = entityMatch[0];
		const tags = {};
		const tagRegex = /<Tag enumID="([^"]+)" name="([^"]+)" type="[^"]+"(?: value="([^"]*)")?[^>]*>([\s\S]*?)<\/Tag>|<Tag enumID="([^"]+)" name="([^"]+)" type="[^"]+" value="([^"]*)"\s*\/>/g;
		for (const tagMatch of body.matchAll(tagRegex)) {
			const name = tagMatch[2] || tagMatch[6];
			const value = tagMatch[3] ?? tagMatch[7] ?? (((tagMatch[4] || "").match(/<enUS>([\s\S]*?)<\/enUS>/) || [])[1]) ?? "";
			tags[name] = singleLine(value);
		}
		entities.set(cardId, {
			id: cardId,
			entityId,
			name: tags.CARDNAME || "",
			text: tags.CARDTEXT || "",
			tags
		});
	}
	return entities;
}

function singleLine(value) {
	return String(value).replace(/<[^>]+>/g, "").replace(/\s+/g, " ").trim();
}

function isNonTraditional(entity) {
	const id = entity.id;
	const set = entity.tags.CARD_SET || "";
	const name = entity.name || "";
	if (set === "1453" || set === "18" || set === "17") return true; // skins, brawl, battlegrounds
	if (/^(BTA|BTA_BOSS|Story|TB|FB|BGS|BG|LOOTA|BOTA|DALA|ULDUM|DRGA|TBA|TUT)_/.test(id)) return true;
	if (id.startsWith("TB_Bacon") || id.startsWith("FB_Champs_")) return true;
	if (/Battlegrounds|Brawl/i.test(name)) return true;
	return false;
}

function isCollectible(entity) {
	return entity.tags.COLLECTIBLE === "1";
}

function setName(entity) {
	const set = entity.tags.CARD_SET || "";
	if (set === "1443") return "Scholomance Academy";
	if (set === "1466") return "Madness at the Darkmoon Faire";
	if (set === "1414") return "Demon Hunter Initiate";
	if (set === "3") return "Basic/Classic/Core legacy";
	return set || "(none)";
}

function bucket(entity) {
	const id = entity.id;
	if (id.startsWith("SCH_")) return "Scholomance Academy";
	if (id.startsWith("DMF_")) return "Madness at the Darkmoon Faire";
	if (id.startsWith("BT_")) return "Ashes of Outland balance/token updates";
	if (id.startsWith("NEW_") || id.startsWith("EX1_")) return "Legacy updates";
	return setName(entity);
}

function main() {
	const oldBuild = process.argv[2] || "48313";
	const newBuild = process.argv[3] || "68600";
	const oldCards = parseCardDefs(path.join(analysisDir, `CardDefs-${oldBuild}.xml`));
	const newCards = parseCardDefs(path.join(analysisDir, `CardDefs-${newBuild}.xml`));

	const added = [];
	const removed = [];
	const changed = [];
	for (const [id, entity] of newCards) {
		if (!oldCards.has(id) && !isNonTraditional(entity)) added.push(entity);
	}
	for (const [id, entity] of oldCards) {
		if (!newCards.has(id) && !isNonTraditional(entity)) removed.push(entity);
	}
	for (const [id, next] of newCards) {
		const prev = oldCards.get(id);
		if (!prev || isNonTraditional(prev) || isNonTraditional(next)) continue;
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
					collectible: isCollectible(next),
					set: setName(next),
					bucket: bucket(next)
				});
			}
		}
	}

	const collectibleAdded = added.filter(isCollectible);
	const tokenAdded = added.filter(e => !isCollectible(e));
	const groups = {};
	for (const entity of added) {
		const key = bucket(entity);
		if (!groups[key]) groups[key] = { collectible: [], support: [] };
		(isCollectible(entity) ? groups[key].collectible : groups[key].support).push(entity);
	}

	const summary = {
		oldBuild,
		newBuild,
		oldEntityCount: oldCards.size,
		newEntityCount: newCards.size,
		addedTraditionalEntities: added.length,
		addedCollectibleCards: collectibleAdded.length,
		addedSupportEntities: tokenAdded.length,
		removedTraditionalEntities: removed.length,
		changedTraditionalFields: changed.length,
		groups,
		removed,
		changed
	};

	fs.writeFileSync(path.join(analysisDir, `annual-${oldBuild}-to-${newBuild}.json`), JSON.stringify(summary, null, 2));

	const lines = [
		`# Annual traditional audit: ${oldBuild} -> ${newBuild}`,
		"",
		"Scope: traditional 1v1. Excludes Battlegrounds, Tavern Brawl, solo adventure/story-only entities, and hero skins.",
		"",
		"## Summary",
		"",
		`- Old entity count: ${oldCards.size}`,
		`- New entity count: ${newCards.size}`,
		`- Added traditional entities: ${added.length}`,
		`- Added collectible cards: ${collectibleAdded.length}`,
		`- Added support entities: ${tokenAdded.length}`,
		`- Removed traditional entities: ${removed.length}`,
		`- Changed traditional fields: ${changed.length}`,
		"",
		"## Added by bucket",
		""
	];

	for (const [key, group] of Object.entries(groups)) {
		lines.push(`### ${key}`, "");
		lines.push(`Collectible: ${group.collectible.length}; support: ${group.support.length}`, "");
		for (const entity of group.collectible) {
			lines.push(`- ${entity.id}: ${entity.name}; type=${entity.tags.CARDTYPE || ""}; class=${entity.tags.CLASS || ""}; cost=${entity.tags.COST || ""}; text=${entity.text || "(no text)"}`);
		}
		if (group.support.length) {
			lines.push("", "Support entities:");
			for (const entity of group.support) {
				lines.push(`- ${entity.id}: ${entity.name}; type=${entity.tags.CARDTYPE || ""}; cost=${entity.tags.COST || ""}; text=${entity.text || "(no text)"}`);
			}
		}
		lines.push("");
	}

	lines.push("## Changed collectible fields", "");
	for (const item of changed.filter(c => c.collectible)) {
		lines.push(`- ${item.id}: ${item.name}; ${item.field}: ${item.old || "(empty)"} -> ${item.new || "(empty)"}; bucket=${item.bucket}`);
	}

	fs.writeFileSync(path.join(analysisDir, `annual-${oldBuild}-to-${newBuild}.md`), lines.join("\n"));
	console.log(`annual-${oldBuild}-to-${newBuild}.json`);
	console.log(`annual-${oldBuild}-to-${newBuild}.md`);
}

main();
