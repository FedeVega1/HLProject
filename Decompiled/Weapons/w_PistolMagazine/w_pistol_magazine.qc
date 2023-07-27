// Created by Crowbar 0.71

$modelname "weapons/w_pistol_magazine.mdl"

$bodygroup "magazine"
{
	studio "w_pistol_magazine.smd"
	blank
}


$surfaceprop "weapon"

$contents "solid"

$illumposition 0.745 -2.144 0.342

$mostlyopaque

$cdmaterials "models\weapons\w_pistol\"

$attachment "muzzle" "ValveBiped.Weapon_bone" 6.31 -0.68 0.42 rotate 4.49 -1.17 -98.82
$attachment "1" "ValveBiped.Weapon_bone" 1.06 0.18 0.72 rotate 8.79 89.53 -85.45

$cbox 0 0 0 0 0 0

$bbox -1.545 -1.048 -3.733 5.776 2.538 4.432

$definebone "ValveBiped.Bip01_R_Hand" "" 1.783728 -5.776747 -3.330453 4.618727 90.659287 -171.24832 0 0 0 0 0 0
$definebone "ValveBiped.Weapon_bone" "ValveBiped.Bip01_R_Hand" 5 -1.5 -5 0 0 0.000001 0 0 0 0 0 0


$sequence "idle" {
	"w_pistol_magazine_anims\idle.smd"
	fadein 0.2
	fadeout 0.2
	fps 1
	loop
}

$collisionmodel "w_pistol_magazine_physics.smd"
{
	$mass 1
	$inertia 1
	$damping 0
	$rotdamping 0
	$rootbone " "

}
