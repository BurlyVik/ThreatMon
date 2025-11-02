public Program() {
    Runtime.UpdateFrequency = UpdateFrequency.Update100;
}

IMyTextSurface lcd;

public void Main(string arg, UpdateType updateSource) {
    if (lcd == null)
        lcd = GridTerminalSystem.GetBlockWithName("Threat[Panel]") as IMyTextSurface;

    if (lcd == null) {
        Echo("LCD 'Threat[Panel]' not found");
        return;
    }

    var blocks = new List<IMyTerminalBlock>();
    GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(blocks);

    float totalThreat = 0;
    string output = "";

    var typeMap = new Dictionary<string, Func<IMyTerminalBlock, bool>>() {
        {"Antennas", b => b is IMyRadioAntenna },
        {"Beacons", b => b is IMyBeacon },
        {"Cargo", b => b is IMyCargoContainer },
        {"Controllers", b => b is IMyShipController },
        {"Gravity", b => b is IMyGravityGenerator },
        {"Guns", b => b is IMyUserControllableGun },
        {"JumpDrives", b => b is IMyJumpDrive },
        {"Mechanical", b => b is IMyMotorStator },
        {"Medical", b => b is IMyMedicalRoom },
        {"NanoBots", b => b.CustomName.Contains("Nanobot") },
        {"Production", b => b is IMyAssembler },
        {"Power", b => b is IMyPowerProducer },
        {"Shields", b => b.CustomName.Contains("Shield") },
        {"Thrusters", b => b is IMyThrust },
        {"Tools", b => b is IMyShipToolBase },
        {"Turrets", b => b is IMyLargeTurretBase },
    };

    var baseVals = new Dictionary<string, float>() {
        {"Antennas", 4f}, {"Beacons", 3f}, {"Cargo", 0.5f}, {"Controllers", 0.5f}, {"Gravity", 2f},
        {"Guns", 5f}, {"JumpDrives", 10f}, {"Mechanical", 1f}, {"Medical", 10f}, {"NanoBots", 15f},
        {"Production", 2f}, {"Power", 0.5f}, {"Shields", 15f}, {"Thrusters", 1f}, {"Tools", 2f}, {"Turrets", 7.5f}
    };

    var modMults = new Dictionary<string, float>() {
        {"Antennas", 2f}, {"Beacons", 2f}, {"Cargo", 2f}, {"Controllers", 2f}, {"Gravity", 4f},
        {"Guns", 4f}, {"JumpDrives", 2f}, {"Mechanical", 2f}, {"Medical", 2f}, {"NanoBots", 2f},
        {"Production", 2f}, {"Power", 2f}, {"Shields", 2f}, {"Thrusters", 2f}, {"Tools", 2f}, {"Turrets", 4f}
    };

    var invScan = new Dictionary<string, bool>() {
        {"Cargo", true}, {"Gravity", true}, {"Guns", true}, {"NanoBots", false},
        {"Production", true}, {"Power", true}, {"Tools", true}, {"Turrets", true}
    };

    var typeCounts = new Dictionary<string, int>();
    var typeScores = new Dictionary<string, float>();

    foreach (var b in blocks) {
        foreach (var kvp in typeMap) {
            if (!kvp.Value(b)) continue;

            string type = kvp.Key;
            if (!typeCounts.ContainsKey(type)) {
                typeCounts[type] = 0;
                typeScores[type] = 0f;
            }

            float val = baseVals[type];
            float mult = b.BlockDefinition.SubtypeName.Contains("Mod") ? modMults[type] : 1f;
            float threat = val * mult;

            if (invScan.ContainsKey(type) && invScan[type] && b.HasInventory) {
                var inv = b.GetInventory();
                if (inv.MaxVolume.RawValue > 0) {
                    float invMod = ((float)inv.CurrentVolume.RawValue / (float)inv.MaxVolume.RawValue) + 1f;
                    if (!float.IsNaN(invMod))
                        threat *= invMod;
                }
            }

            typeCounts[type]++;
            typeScores[type] += threat;
            break;
        }
    }

    foreach (var type in typeCounts.Keys) {
        float threat = typeScores[type];
        int count = typeCounts[type];
        output += PadRight(type, 12) + $"{count}x: {threat:F2}\n";
        totalThreat += threat;
    }

    float gridSizeMult = 0f;
    foreach (var b in blocks) {
        var cube = b.CubeGrid;
        gridSizeMult += (cube.GridSizeEnum == MyCubeSize.Large ? 2.5f : 0.5f) * (cube.IsStatic ? 0.75f : 1f);
    }

    float avgMult = blocks.Count > 0 ? (gridSizeMult / blocks.Count) : 1f;
    float final = totalThreat * avgMult * 0.70f;

    output += "\nFinal Threat: " + final.ToString("F2");

    lcd.ContentType = ContentType.TEXT_AND_IMAGE;
    lcd.FontSize = 1.0f;
    lcd.Alignment = TextAlignment.LEFT;
    lcd.WriteText(output);
    Echo(output);
}

string PadRight(string input, int totalWidth) {
    while (input.Length < totalWidth)
        input += " ";
    return input;
}
