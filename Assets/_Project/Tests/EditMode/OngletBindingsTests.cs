using System.Collections.Generic;
using Bocage.Presentation.Bindings;
using Bocage.Presentation.Scene.Fauna;
using NUnit.Framework;
using UnityEngine;

namespace Bocage.Tests.EditMode
{
    /// <summary>
    /// EditMode coverage du seul helper pur restant des panneaux Niveau B :
    /// <see cref="OngletBiodivBinding.CountDistinctSpecies"/>. Les agrégats
    /// météo (T° moyenne / cumul pluie) et la décompo économique (PSE/PAC/MAEC/…)
    /// vivent désormais dans la session et <c>EconomyRule.Breakdown</c>, couverts par
    /// <c>S4DataTests</c> — leurs anciens tests de binding sont retirés.
    /// </summary>
    public sealed class OngletBiodivBindingTests
    {
        [Test]
        public void CountDistinctSpecies_CountsUniqueIgnoringDuplicatesAndNulls()
        {
            var a = ScriptableObject.CreateInstance<FaunaSpeciesDefinition>();
            var b = ScriptableObject.CreateInstance<FaunaSpeciesDefinition>();
            var buffer = new List<FaunaSpeciesDefinition>();
            var visible = new List<FaunaSpeciesDefinition> { a, b, a, null, b };

            Assert.AreEqual(2, OngletBiodivBinding.CountDistinctSpecies(visible, buffer));

            Object.DestroyImmediate(a);
            Object.DestroyImmediate(b);
        }

        [Test]
        public void CountDistinctSpecies_EmptyOrNull_ReturnsZero()
        {
            var buffer = new List<FaunaSpeciesDefinition>();
            Assert.AreEqual(0, OngletBiodivBinding.CountDistinctSpecies(new List<FaunaSpeciesDefinition>(), buffer));
            Assert.AreEqual(0, OngletBiodivBinding.CountDistinctSpecies(null, buffer));
        }
    }
}
