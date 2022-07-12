// Created by Crowbar 0.71

$modelname "weapons/v_Pistol.mdl"

$bodygroup "studio"
{
	studio "v_pistol_Pistol_reference.smd"
}
$bodygroup "hands"
{
	studio "v_pistol_RefHEVHands.smd"
}


$surfaceprop "default"

$contents "solid"

$illumposition -2.641 -8.852 -9.508

$cdmaterials "models\weapons\v_hand\"
$cdmaterials "models\Weapons\v_pistol\"

$attachment "muzzle" "usp" -6.7 0.6 -0.03 rotate 0 180 90
$attachment "1" "usp" -1 0.75 -0.05 rotate 25 90 25

$cbox 0 0 0 0 0 0

$bbox -10.334 -14.66 -18.016 28.039 9.377 -1

$definebone "usp" "" -0.022445 -21.955212 3.303217 0 89.999982 89.999982 0 0 0 0 0 0
$definebone "l_wrist" "" 8.27366 -15.41854 -0.926869 0.417457 -90.645189 175.772678 0 0 0 0 0 0
$definebone "l_thumb_low" "l_wrist" 1.473658 0.639411 -1.295237 42.064956 38.432467 121.667239 0 0 0 0 0 0
$definebone "l_thumb_mid" "l_thumb_low" 1.832611 0 0.000003 2.721014 43.433921 178.190961 0 0 0 0 0 0
$definebone "l_thumb_tip" "l_thumb_mid" 1.003334 0 -0.000001 -0.825156 -9.614528 -174.182419 0 0 0 0 0 0
$definebone "l_index_low" "l_wrist" 3.823887 -0.4196 -1.411513 20.645222 -2.816888 13.451444 0 0 0 0 0 0
$definebone "l_index_mid" "l_index_low" 2.187963 -0.000001 0.000001 -6.804099 21.420182 -1.453649 0 0 0 0 0 0
$definebone "l_index_tip" "l_index_mid" 1.100958 0.000001 0 -4.874949 14.690431 3.264833 0 0 0 0 0 0
$definebone "l_middle_low" "l_wrist" 3.987879 -0.577446 -0.474705 4.54648 21.596623 12.497815 0 0 0 0 0 0
$definebone "l_middle_mid" "l_middle_low" 1.890094 -0.000003 0 -8.9056 24.700507 4.0193 0 0 0 0 0 0
$definebone "l_middle_tip" "l_middle_mid" 1.310922 0.000004 0 4.392404 23.589399 1.30927 0 0 0 0 0 0
$definebone "l_ring_low" "l_wrist" 4.117973 -0.654523 0.453703 -10.081136 29.584261 6.062067 0 0 0 0 0 0
$definebone "l_ring_mid" "l_ring_low" 1.65528 0.000006 0 5.094914 25.61631 -6.19459 0 0 0 0 0 0
$definebone "l_ring_tip" "l_ring_mid" 0.869585 0.000002 0 -1.823612 12.490333 3.304021 0 0 0 0 0 0
$definebone "l_pinky_low" "l_wrist" 4.150646 -0.581573 1.354871 -13.382863 36.847546 -17.041387 0 0 0 0 0 0
$definebone "l_pinky_mid" "l_pinky_low" 1.157246 0.000002 0 2.245369 28.080494 1.995552 0 0 0 0 0 0
$definebone "l_pinky_tip" "l_pinky_mid" 1.019838 -0.000004 0 -2.924945 22.690901 -84.090905 0 0 0 0 0 0
$definebone "l_upperarm" "" 8 11 1.176544 6.11564 -88.969286 -90.436691 0 0 0 0 0 0
$definebone "l_forearm" "l_upperarm" 12.695415 0.000001 -0.000001 0.84179 -2.750713 4.42914 0 0 0 0 0 0
$definebone "r_wrist" "" -8.338264 -15.418537 -0.926869 0.417457 -89.35483 175.913093 0 0 0 0 0 0
$definebone "r_thumb_low" "r_wrist" 1.474217 -0.497159 -1.376391 46.815052 -31.793175 68.019545 0 0 0 0 0 0
$definebone "r_thumb_mid" "r_thumb_low" 1.832171 0.000001 -0.000004 -7.472109 41.829537 -146.166035 0 0 0 0 0 0
$definebone "r_thumb_tip" "r_thumb_mid" 1.042036 0.000001 0.000003 -18.307728 0.094432 -169.299284 0 0 0 0 0 0
$definebone "r_index_low" "r_wrist" 3.823316 0.6656 -1.331303 20.740728 0.24047 13.988649 0 0 0 0 0 0
$definebone "r_index_mid" "r_index_low" 2.152369 0.000003 0.000001 -13.241398 -24.701234 7.08806 0 0 0 0 0 0
$definebone "r_index_tip" "r_index_mid" 1.056623 0.000002 0 -9.788765 -8.183664 12.944545 0 0 0 0 0 0
$definebone "r_middle_low" "r_wrist" 3.985126 0.882023 -0.368497 7.385138 -15.400762 8.286403 0 0 0 0 0 0
$definebone "r_middle_mid" "r_middle_low" 1.83018 0 0 -13.181009 -47.169846 11.228945 0 0 0 0 0 0
$definebone "r_middle_tip" "r_middle_mid" 1.561354 -0.000002 0 -1.59173 -15.763102 0.346572 0 0 0 0 0 0
$definebone "r_ring_low" "r_wrist" 4.116523 0.710978 0.55226 -5.152439 -35.605213 15.897752 0 0 0 0 0 0
$definebone "r_ring_mid" "r_ring_low" 1.75272 0 0 -0.555424 -21.043932 2.296701 0 0 0 0 0 0
$definebone "r_ring_tip" "r_ring_mid" 0.869359 0 0 -3.772121 -12.055361 -2.081266 0 0 0 0 0 0
$definebone "r_pinky_low" "r_wrist" 4.118823 0.330451 1.420007 -6.546673 -37.018174 17.520533 0 0 0 0 0 0
$definebone "r_pinky_mid" "r_pinky_low" 1.17632 0.000004 0 6.847419 -9.636692 7.122838 0 0 0 0 0 0
$definebone "r_pinky_tip" "r_pinky_mid" 0.970201 -0.000002 0 -0.113048 -32.870361 97.429985 0 0 0 0 0 0
$definebone "r_upperarm" "" -7.009326 11 1.176544 6.089683 -92.524031 -88.189948 0 0 0 0 0 0
$definebone "r_forearm" "r_upperarm" 12.706345 0.000001 0 0.842993 -2.744525 4.600393 0 0 0 0 0 0
$definebone "usp_trigger" "usp" -0.735476 -1.133394 0.022445 0 0 0 0 0 0 0 0 0
$definebone "usp_slide" "usp" -3.817837 0.392731 -0.017613 0 0 0 0 0 0 0 0 0
$definebone "usp_slidelock" "usp" -1.015219 -0.450641 0.659471 0 4.137271 0 0 0 0 0 0 0
$definebone "usp_mag" "usp" 2.113039 -3.857967 -0.032198 0 103.526024 0 0 0 0 0 0 0
$definebone "usp_magbullet" "usp_mag" 3.850047 -0.721819 0.028026 0 76.474001 -0.000115 0 0 0 0 0 0
$definebone "usp_hammer" "usp" 3.386322 -0.139487 -0.008962 0 43.689121 0 0 0 0 0 0 0


$sequence "idle" {
	"v_pistol_anims\idle.smd"
	activity "ACT_VM_IDLE" 1
	fadein 0.2
	fadeout 0.2
	node "0"
	fps 30
	loop
}

$sequence "idle_is" {
	"v_pistol_anims\idle_is.smd"
	activity "ACT_VM_IDLE_SILENCED" 1
	fadein 0.2
	fadeout 0.2
	node "0"
	fps 30
	loop
}

$sequence "holster" {
	"v_pistol_anims\holster.smd"
	activity "ACT_VM_HOLSTER" 1
	fadein 0.2
	fadeout 0.2
	node "0"
	fps 30
}

$sequence "fire1" {
	"v_pistol_anims\fire1.smd"
	activity "ACT_VM_PRIMARYATTACK" 1
	{ event AE_MUZZLEFLASH 0 "PISTOL MUZZLE" }
	{ event 6001 0 "0" }
	fadein 0.2
	fadeout 0.2
	node "2"
	snap
	fps 30
}

$sequence "fire2" {
	"v_pistol_anims\fire2.smd"
	activity "ACT_VM_RECOIL1" 1
	{ event AE_MUZZLEFLASH 0 "PISTOL MUZZLE" }
	{ event 6001 0 "0" }
	fadein 0.2
	fadeout 0.2
	node "2"
	snap
	fps 30
}

$sequence "fire3" {
	"v_pistol_anims\fire3.smd"
	activity "ACT_VM_RECOIL2" 1
	{ event AE_MUZZLEFLASH 0 "PISTOL MUZZLE" }
	{ event 6001 0 "0" }
	fadein 0.2
	fadeout 0.2
	node "2"
	snap
	fps 30
}

$sequence "fire4" {
	"v_pistol_anims\fire4.smd"
	activity "ACT_VM_RECOIL3" 1
	{ event AE_MUZZLEFLASH 0 "PISTOL MUZZLE" }
	{ event 6001 0 "0" }
	fadein 0.2
	fadeout 0.2
	node "2"
	snap
	fps 30
}

$sequence "fire1_is" {
	"v_pistol_anims\fire1_is.smd"
	activity "ACT_VM_PRIMARYATTACK_IRONSIGHTED1" 1
	{ event AE_MUZZLEFLASH 0 "PISTOL MUZZLE" }
	{ event 6001 0 "0" }
	fadein 0.1
	fadeout 0.1
	node "2"
	fps 30
}

$sequence "fire2_is" {
	"v_pistol_anims\fire2_is.smd"
	activity "ACT_VM_PRIMARYATTACK_IRONSIGHTED2" 1
	{ event AE_MUZZLEFLASH 0 "PISTOL MUZZLE" }
	{ event 6001 0 "0" }
	fadein 0.1
	fadeout 0.1
	node "2"
	fps 30
}

$sequence "fire3_is" {
	"v_pistol_anims\fire3_is.smd"
	activity "ACT_VM_PRIMARYATTACK_IRONSIGHTED3" 1
	{ event AE_MUZZLEFLASH 0 "PISTOL MUZZLE" }
	{ event 6001 0 "0" }
	fadein 0.1
	fadeout 0.1
	node "2"
	fps 30
}

$sequence "fire_last" {
	"v_pistol_anims\fire_last.smd"
	activity "ACT_VM_DRYFIRE" 1
	fadein 0.2
	fadeout 0.2
	node "2"
	snap
	fps 30
}

$sequence "shoot_last" {
	"v_pistol_anims\shoot_last.smd"
	activity "ACT_VM_SHOOTLAST" 1
	fadein 0.2
	fadeout 0.2
	node "2"
	snap
	fps 30
}

$sequence "draw" {
	"v_pistol_anims\draw.smd"
	activity "ACT_VM_DRAW" 1
	{ event 5004 0 "Weapon_Pistol.Draw" }
	{ event 5004 14 "Weapon_USP.SlideRelease" }
	fadein 0.2
	fadeout 0.2
	node "0"
	snap
	fps 30
}

$sequence "reload" {
	"v_pistol_anims\reload.smd"
	activity "ACT_VM_RELOAD" 1
	{ event 3015 10 "muzzle" }
	{ event 5004 3 "Weapon_USP.SlidePull" }
	{ event 5004 9 "Weapon_USP.ClipOut" }
	{ event 5004 25 "Weapon_USP.ClipIn" }
	{ event 5004 36 "Weapon_USP.SlideRelease" }
	{ event 5004 1 "Weapon_Generic.Movement5" }
	{ event 5004 22 "Weapon_Generic.Movement2" }
	{ event 5004 32 "Weapon_Generic.Movement4" }
	fadein 0.2
	fadeout 0.2
	node "Fire"
	fps 30
}

$sequence "idletolow" {
	"v_pistol_anims\idletolow.smd"
	activity "ACT_VM_LOWERED_TO_IDLE" 1
	fadein 0.2
	fadeout 0.2
	transition "0" "1"
	fps 40
}

$sequence "lowtoidle" {
	"v_pistol_anims\lowtoidle.smd"
	activity "ACT_VM_IDLE_TO_LOWERED" 1
	fadein 0.2
	fadeout 0.2
	transition "1" "0"
	fps 40
}

$sequence "lowidle" {
	"v_pistol_anims\lowidle.smd"
	activity "ACT_VM_IDLE_LOWERED" 1
	fadein 0.2
	fadeout 0.2
	node "1"
	fps 30
	loop
}

$sequence "Walk" {
	"v_pistol_anims\Walk.smd"
	activity "ACT_VM_WALK" 1
	{ event 5004 0 "Weapon_Generic.Walk1" }
	{ event 5004 15 "Weapon_Generic.Walk2" }
	fadein 0.35
	fadeout 0.35
	transition "Ready" "Motion"
	fps 30
	loop
}

$sequence "Sprint" {
	"v_pistol_anims\Sprint.smd"
	activity "ACT_VM_SPRINT" 1
	{ event 5004 1 "Weapon_Generic.Sprint1" }
	{ event 5004 13 "Weapon_Generic.Sprint2" }
	fadein 0.35
	fadeout 0.35
	transition "Ready" "Motion"
	fps 34
	loop
}

$sequence "inspect1" {
	"v_pistol_anims\inspect1.smd"
	activity "ACT_VM_INSPECT" 1
	{ event 5004 3 "Weapon_Generic.Movement1" }
	{ event 5004 31 "Weapon_Generic.Movement2" }
	{ event 5004 56 "Weapon_Generic.Movement5" }
	fadein 0.4
	fadeout 0.4
	node "Ready"
	fps 30
}

$sequence "inspect2" {
	"v_pistol_anims\inspect2.smd"
	activity "ACT_VM_INSPECT_ALT" 1
	{ event 5004 1 "Weapon_Generic.Movement2" }
	{ event 5004 24 "Weapon_USP.SlidePull" }
	{ event 5004 52 "Weapon_USP.SlideRelease" }
	{ event 5004 53 "Weapon_Generic.Movement5" }
	fadein 0.4
	fadeout 0.4
	node "Ready"
	fps 30
}
