using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace EquipmentSimulator.Core.Models
{
    /// <summary>
    /// Represents a formal semiconductor manufacturing process recipe (Process Program / PPID).
    /// Contains the ordered sequence of process steps and target physical operating setpoints.
    /// </summary>
    public class Recipe
    {
        public string RecipeId { get; }
        public string RecipeName { get; }
        public string Description { get; }
        public string LayerName { get; }
        public IReadOnlyList<ProcessStep> Steps { get; }
        public double TotalNominalDurationSeconds { get; }
        public double ExposureDoseMilliJoules { get; }
        public double TargetVacuumTorr { get; }
        public double TargetTemperatureCelsius { get; }
        public int DefaultWafersPerLot { get; }

        public Recipe(
            string recipeId,
            string recipeName,
            string description,
            string layerName,
            IEnumerable<ProcessStep> steps,
            double exposureDoseMilliJoules = 45.0,
            double targetVacuumTorr = 1.0e-6,
            double targetTemperatureCelsius = 21.50,
            int defaultWafersPerLot = 25)
        {
            if (string.IsNullOrWhiteSpace(recipeId))
                throw new ArgumentException("RecipeId cannot be null or empty.", nameof(recipeId));

            RecipeId = recipeId.Trim();
            RecipeName = recipeName ?? RecipeId;
            Description = description ?? string.Empty;
            LayerName = layerName ?? "Generic-Layer";
            ExposureDoseMilliJoules = exposureDoseMilliJoules;
            TargetVacuumTorr = targetVacuumTorr;
            TargetTemperatureCelsius = targetTemperatureCelsius;
            DefaultWafersPerLot = defaultWafersPerLot > 0 ? defaultWafersPerLot : 25;

            var stepList = steps?.ToList() ?? new List<ProcessStep>();
            if (stepList.Count == 0)
            {
                stepList = CreateDefaultSteps(exposureDoseMilliJoules, targetVacuumTorr, targetTemperatureCelsius);
            }

            Steps = new ReadOnlyCollection<ProcessStep>(stepList);
            TotalNominalDurationSeconds = Steps.Sum(s => s.DurationSeconds);
        }

        private static List<ProcessStep> CreateDefaultSteps(double dose, double vacuum, double temp)
        {
            return new List<ProcessStep>
            {
                new ProcessStep(ProcessStepType.WaferLoad, "Wafer Load & Align", "Atmospheric transfer and optical pre-alignment", 2.0, 760.0, 0.0, temp),
                new ProcessStep(ProcessStepType.PumpDown, "Chamber Evacuation", "Roughing and turbo pumpdown to high vacuum", 3.0, vacuum, 0.0, temp),
                new ProcessStep(ProcessStepType.Exposure, "Laser Exposure Scan", "ArF/KrF pulsed laser reticle scan exposure", 4.0, vacuum, dose, temp),
                new ProcessStep(ProcessStepType.ChamberVent, "Chamber N2 Venting", "Nitrogen purge and repressurization to 760 Torr", 2.5, 760.0, 0.0, temp),
                new ProcessStep(ProcessStepType.WaferUnload, "Wafer Unload", "Chamber chuck transfer back to FOUP cassette", 1.5, 760.0, 0.0, temp)
            };
        }

        #region Standard Factory Recipes

        /// <summary>
        /// Standard 193nm Deep Ultraviolet (DUV) immersion lithography recipe for 5nm node polysilicon gate patterning.
        /// </summary>
        public static Recipe CreateStandardLithographyRecipe()
        {
            return new Recipe(
                "RECIPE-POLY-GATE-N5",
                "DUV Immersion Poly-Gate Lithography",
                "Pulsed 193nm ArF exposure for sub-5nm poly silicon gate pattern definition.",
                "Poly-Silicon-Gate",
                null,
                exposureDoseMilliJoules: 45.0,
                targetVacuumTorr: 1.0e-6,
                targetTemperatureCelsius: 21.50,
                defaultWafersPerLot: 25);
        }

        /// <summary>
        /// High-NA EUV lithography recipe for 3nm node Metal-1 interconnect trench patterning.
        /// </summary>
        public static Recipe CreateEuvHighNumericalApertureRecipe()
        {
            return new Recipe(
                "RECIPE-EUV-MET1-3NM",
                "High-NA EUV Metal-1 Trench Patterning",
                "Extreme Ultraviolet (13.5nm) High-NA scanner recipe with ultra-low vacuum chuck stabilization.",
                "Metal-1-Interconnect",
                new List<ProcessStep>
                {
                    new ProcessStep(ProcessStepType.WaferLoad, "Dual-Pod Load & Interferometer Align", "Vacuum load lock transfer and laser interferometry alignment", 2.5, 760.0, 0.0, 21.50),
                    new ProcessStep(ProcessStepType.PumpDown, "Ultra-High Vacuum Cryo-Pumping", "Cryogenic turbo pump cycle to 5.0e-7 Torr", 3.5, 5.0e-7, 0.0, 21.50),
                    new ProcessStep(ProcessStepType.Exposure, "EUV Source Scan (13.5nm)", "Plasma tin droplet EUV laser exposure dose scan", 5.0, 5.0e-7, 62.5, 21.50),
                    new ProcessStep(ProcessStepType.ChamberVent, "Ultra-Pure N2 Soft Vent", "Controlled soft vent back to transfer pressure", 3.0, 760.0, 0.0, 21.50),
                    new ProcessStep(ProcessStepType.WaferUnload, "FOUP Return & Lot Verify", "Atmospheric robot arm return and barcode verify", 2.0, 760.0, 0.0, 21.50)
                },
                exposureDoseMilliJoules: 62.5,
                targetVacuumTorr: 5.0e-7,
                targetTemperatureCelsius: 21.50,
                defaultWafersPerLot: 25);
        }

        /// <summary>
        /// Deep Reactive Ion Etch (DRIE) recipe for Through-Silicon-Via (TSV) high aspect ratio anisotropic etching.
        /// </summary>
        public static Recipe CreateDeepReactiveIonEtchRecipe()
        {
            return new Recipe(
                "RECIPE-DRIE-TSV-DEEP",
                "Bosch Process DRIE TSV Etch",
                "Alternating SF6 etch and C4F8 passivation cycles for high aspect ratio silicon via formation.",
                "TSV-Via-Formation",
                new List<ProcessStep>
                {
                    new ProcessStep(ProcessStepType.WaferLoad, "Electrostatic Chuck Clamp", "Robotic wafer transfer and He backside cooling clamp", 2.0, 760.0, 0.0, 20.00),
                    new ProcessStep(ProcessStepType.PumpDown, "Process Chamber Roughing", "Turbo pump evacuation to 2.5e-3 Torr process pressure", 2.5, 2.5e-3, 0.0, 20.00),
                    new ProcessStep(ProcessStepType.Exposure, "ICP-RIE Plasma Discharge", "Inductively Coupled Plasma RF power 1500W etching", 4.5, 2.5e-3, 90.0, 20.00),
                    new ProcessStep(ProcessStepType.ChamberVent, "Chamber N2 Purge", "Gas line purge and chamber vent cycle", 2.5, 760.0, 0.0, 20.00),
                    new ProcessStep(ProcessStepType.WaferUnload, "De-Clamp & FOUP Return", "Electrostatic discharge, chuck release, and FOUP deposit", 1.5, 760.0, 0.0, 20.00)
                },
                exposureDoseMilliJoules: 90.0,
                targetVacuumTorr: 2.5e-3,
                targetTemperatureCelsius: 20.00,
                defaultWafersPerLot: 25);
        }

        #endregion

        public override string ToString() => $"{RecipeId} ({RecipeName}) [{Steps.Count} Steps, {TotalNominalDurationSeconds:F1}s/wafer]";
    }
}
