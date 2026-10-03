using System;
using EquipmentSimulator.Core.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EquipmentSimulator.Core.Tests
{
    [TestClass]
    public class RecipeTests
    {
        [TestMethod]
        public void StandardLithographyRecipe_InitializesWithCorrectParameters()
        {
            var recipe = Recipe.CreateStandardLithographyRecipe();

            Assert.AreEqual("RECIPE-POLY-GATE-N5", recipe.RecipeId);
            Assert.AreEqual(5, recipe.Steps.Count);
            Assert.AreEqual(45.0, recipe.ExposureDoseMilliJoules);
            Assert.AreEqual(1.0e-6, recipe.TargetVacuumTorr);
            Assert.AreEqual(21.50, recipe.TargetTemperatureCelsius);
            Assert.AreEqual(25, recipe.DefaultWafersPerLot);
            Assert.IsTrue(recipe.TotalNominalDurationSeconds > 0);
        }

        [TestMethod]
        public void EuvRecipe_HasHigherDoseAndLowVacuum()
        {
            var recipe = Recipe.CreateEuvHighNumericalApertureRecipe();

            Assert.AreEqual("RECIPE-EUV-MET1-3NM", recipe.RecipeId);
            Assert.AreEqual(5, recipe.Steps.Count);
            Assert.AreEqual(62.5, recipe.ExposureDoseMilliJoules);
            Assert.AreEqual(5.0e-7, recipe.TargetVacuumTorr);
        }

        [TestMethod]
        public void DrieRecipe_HasCorrectParameters()
        {
            var recipe = Recipe.CreateDeepReactiveIonEtchRecipe();

            Assert.AreEqual("RECIPE-DRIE-TSV-DEEP", recipe.RecipeId);
            Assert.AreEqual(5, recipe.Steps.Count);
            Assert.AreEqual(2.5e-3, recipe.TargetVacuumTorr);
            Assert.AreEqual(20.00, recipe.TargetTemperatureCelsius);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void RecipeConstructor_EmptyRecipeId_ThrowsArgumentException()
        {
            new Recipe("", "Test", "Test", "Layer", null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void ProcessStep_ZeroDuration_ThrowsException()
        {
            new ProcessStep(ProcessStepType.WaferLoad, "Invalid", "Desc", 0.0);
        }
    }
}
