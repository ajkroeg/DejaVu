# DejaVu

This is a relatively simple mod that lets player face their own creations in combat. Whenever the player drops a unit, the MechDef for that unit is saved and, on mission complete, exported to MechDef and ChassisDef jsons that will then be loaded as normal mech variants the next time the game is loaded and added to the potential spawn pool.

Before export, the custom mechs are checked against existing mechs in order to avoid duplication. Variant names are also appended with a random 2-character string to further distinguish these mechs when encountered in the wild. E.g., a player-customized variant of the Hunchback HBK-4G might be saved as HBK-4G-2W. These custom mechs will have the same unit tags as their originating variant.

## Settings

```
	"Settings": {
		"enableLogging": true,
		"trace": false,
		"killsToSave": 0,
		"dissallowedComponentTags": [
			"range_standard"
		]
	}
```
`enableLogging`: bool, enables logging

`trace`: bool, enables verbose logging (not recommended to enable)

`killsToSave`: int, set a minimum number of kills a unit must get during the mission in order to be exported.

`dissallowedComponentTags`: List of mech ComponentTags which, if present on the mech, will prevent that mech from being exported.
