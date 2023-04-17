# DejaVu

**v1.1.0.0 and higher requires modtek v3 or higher**

This is a relatively simple mod that lets player face their own creations in combat. Whenever the player drops a unit, the MechDef for that unit is saved and, on mission complete, exported to MechDef and ChassisDef jsons that will then be loaded as normal mech variants the next time the game is loaded and added to the potential spawn pool.

Before export, the custom mechs are checked against existing mechs in order to avoid duplication. Variant names are also appended with a random 2-character string to further distinguish these mechs when encountered in the wild. E.g., a player-customized variant of the Hunchback HBK-4G might be saved as HBK-4G-2W. These custom mechs will have the same unit tags as their originating variant. Finally, the mech inventory of the custom mech is checked against the inventory of all existing variants; if a duplicate is found, the mech is not exported.

<b>Depends On CustomComponents and CustomUnits</b>

## Settings

```
	"Settings": {
		"enableLogging": true,
		"trace": false,
		"killsToSave": 0,
		"enableMechBayExport": true,
		"dissallowedComponentTags": [
			"range_standard"
			],
		"clearMechTags": true,
		"customChassisTags":[
			"thisisacustomChassisTag"
			],
		"customMechTags":[
			"thisisacustomMechTag"
			]
	}
```

`enableLogging`: bool, enables logging

`trace`: bool, enables verbose logging (not recommended to enable)

`killsToSave`: int, set a minimum number of kills a unit must get during the mission in order to be exported. If set to -1, all exporting of units dropped in contracts is disabled.

`enableMechBayExport`: bool. if true; holding shift while clicking "validate" in the MechLab will manually export that unit <i>if</i> that unit is a valid loadout (no "unfieldable" warnings and mech name is not blank).

`disallowedComponentTags`: List of mech ComponentTags which, if present on the mech, will prevent that mech from being exported.

`clearMechTags`: bool. if true, all MechTags and ChassisTags will be cleared from the exported unit.

`customChassisTags`:  List<string>; if `clearMechTags` = true, the exported mechs ChassisTags will be replaced with these

`customMechTags`:  List<string>; if `clearMechTags` = true, the exported mechs MechTags will be replaced with these
