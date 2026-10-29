#if UNITY_EDITOR
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace VhsInheritance.Tests
{
    public sealed class VhsPrototypeTests
    {
        [Test]
        public void DefaultConfigContainsTenUpgradeTracks()
        {
            VhsGameConfig gameConfig = ScriptableObject.CreateInstance<VhsGameConfig>();
            gameConfig.ResetToPrototypeDefaults();

            Assert.That(gameConfig.upgrades, Has.Count.EqualTo(10));
            Assert.That(gameConfig.dayDurationSeconds, Is.EqualTo(480f));
            Assert.That(gameConfig.GetTrack(UpgradeType.CarryCapacity).values, Is.EquivalentTo(new[] { 1f, 2f, 4f }));
            Assert.That(gameConfig.orderMaximumByDay, Is.EqualTo(new[] { 3, 5, 7, 10 }));
        }

        [Test]
        public void SaveDataRoundTripsThroughJson()
        {
            VhsSaveData source = new VhsSaveData
            {
                totalMoney = 123,
                currentDayIndex = 4,
                machineOverflow = 17,
                foreignCassetteReceivedToday = true
            };
            source.tutorialFlagsShown.Add("day_wasd");
            source.pile.Add(new CassetteData { id = "cassette", genre = CassetteGenre.Comedy });

            string json = JsonUtility.ToJson(source);
            VhsSaveData restored = JsonUtility.FromJson<VhsSaveData>(json);

            Assert.That(restored.totalMoney, Is.EqualTo(123));
            Assert.That(restored.currentDayIndex, Is.EqualTo(4));
            Assert.That(restored.machineOverflow, Is.EqualTo(17));
            Assert.That(restored.pile[0].genre, Is.EqualTo(CassetteGenre.Comedy));
            Assert.That(restored.tutorialFlagsShown, Does.Contain("day_wasd"));
        }

        [Test]
        public void AssistantPathfinderRoutesAroundObstacle()
        {
            Rect bounds = new Rect(-4f, -3f, 8f, 6f);
            Rect obstacle = new Rect(-0.75f, -2f, 1.5f, 4f);
            List<Vector2> path = VhsGridPathfinder.FindPath(new Vector2(-3f, 0f), new Vector2(3f, 0f), bounds, 0.5f, new[] { obstacle });

            Assert.That(path.Count, Is.GreaterThan(2));
            for (int i = 1; i < path.Count - 1; i++)
            {
                Assert.That(obstacle.Contains(path[i]), Is.False);
            }
        }

        [Test]
        public void AssistantDoesNotWalkThroughASealedWall()
        {
            Rect bounds = new Rect(-4f, -3f, 8f, 6f);
            var wall = new Rect(-0.75f, -3f, 1.5f, 6f);
            var path = VhsGridPathfinder.FindPath(new Vector2(-3, 0), new Vector2(3, 0), bounds, 0.5f, new[] { wall });
            Assert.That(path, Is.Empty);
        }

        [Test]
        public void SaveKeepsReturnedCassetteIdentityAndAssistantInventory()
        {
            var data = new VhsSaveData();
            data.machineStoredCassettes.Add(new CassetteData { id = "paid-cassette", genre = CassetteGenre.Drama, placementRewardClaimed = true });
            data.assistantHeldCassette = new CassetteData { id = "assistant-cassette", genre = CassetteGenre.Audio };
            var restored = JsonUtility.FromJson<VhsSaveData>(JsonUtility.ToJson(data));
            Assert.That(restored.machineStoredCassettes[0].id, Is.EqualTo("paid-cassette"));
            Assert.That(restored.machineStoredCassettes[0].placementRewardClaimed, Is.True);
            Assert.That(restored.assistantHeldCassette.genre, Is.EqualTo(CassetteGenre.Audio));
        }
    }
}
#endif
