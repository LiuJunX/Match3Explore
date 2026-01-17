using Match3.Core.Models.Enums;
using Match3.Editor.ViewModels;
using Match3.Editor.Helpers;

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
    }
}

