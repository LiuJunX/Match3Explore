using Match3.Core.Models.Enums;
using Match3.Editor.ViewModels;
using Match3.Editor.Helpers;
using Match3.Editor.Logic;
using Match3.Core.Config;

namespace Match3.Editor.Tests
{
    public class LevelEditorViewModelPaletteTests
    {
        [Fact]
        public void TilePaletteTypes_ShouldContainExpectedColorsAndNone()
        {
            var types = LevelEditorViewModel.TilePaletteTypes;

            Assert.Contains(TileType.Red, types);
            Assert.Contains(TileType.Green, types);
            Assert.Contains(TileType.Blue, types);
            Assert.Contains(TileType.Yellow, types);
            Assert.Contains(TileType.Purple, types);
            Assert.Contains(TileType.Orange, types);
            Assert.Contains(TileType.Rainbow, types);
            Assert.Contains(TileType.None, types);
            Assert.Equal(8, types.Count);
        }

        [Fact]
        public void GetTileBackground_ShouldReturnSolidColorOrGradient()
        {
            var red = EditorStyleHelper.GetTileColor(TileType.Red);
            var none = EditorStyleHelper.GetTileColor(TileType.None);
            var rainbow = EditorStyleHelper.GetTileColor(TileType.Rainbow);

            Assert.Equal("#dc3545", red);
            Assert.Equal("#f8f9fa", none);
            Assert.Contains("linear-gradient", rainbow);
        }

        [Fact]
        public void GetTileCheckmarkClass_ShouldUseDarkTextOnLightBackgrounds()
        {
            var noneClass = EditorStyleHelper.GetTileCheckmarkClass(TileType.None);
            var yellowClass = EditorStyleHelper.GetTileCheckmarkClass(TileType.Yellow);
            var redClass = EditorStyleHelper.GetTileCheckmarkClass(TileType.Red);

            Assert.Equal("text-dark", noneClass);
            Assert.Equal("text-dark", yellowClass);
            Assert.Equal("text-white", redClass);
        }

        [Fact]
        public void GridManipulator_PaintCover_WithCustomHP_ShouldSetHealth()
        {
            var manipulator = new GridManipulator();
            var config = new LevelConfig(3, 3);

            manipulator.PaintCover(config, 0, CoverType.IceCover, 1);
            Assert.Equal(CoverType.IceCover, config.Covers[0]);
            Assert.Equal(1, config.CoverHealths[0]);

            manipulator.PaintCover(config, 1, CoverType.IceCover, 2);
            Assert.Equal(CoverType.IceCover, config.Covers[1]);
            Assert.Equal(2, config.CoverHealths[1]);

            manipulator.PaintCover(config, 2, CoverType.IceCover, 3);
            Assert.Equal(CoverType.IceCover, config.Covers[2]);
            Assert.Equal(3, config.CoverHealths[2]);
        }

        [Fact]
        public void GridManipulator_PaintCover_WithZeroHP_ShouldUseDefaultHealth()
        {
            var manipulator = new GridManipulator();
            var config = new LevelConfig(3, 3);

            manipulator.PaintCover(config, 0, CoverType.IceCover, 0);
            Assert.Equal(CoverType.IceCover, config.Covers[0]);
            Assert.Equal(2, config.CoverHealths[0]); // Default HP for IceCover is 2
        }

        [Fact]
        public void GridManipulator_ClearCover_ShouldResetHealthToZero()
        {
            var manipulator = new GridManipulator();
            var config = new LevelConfig(3, 3);

            manipulator.PaintCover(config, 0, CoverType.IceCover, 3);
            Assert.Equal(3, config.CoverHealths[0]);

            manipulator.ClearCover(config, 0);
            Assert.Equal(CoverType.None, config.Covers[0]);
            Assert.Equal(0, config.CoverHealths[0]);
        }
    }
}

