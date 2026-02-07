#if UNITY_EDITOR
namespace Match3.Unity.Views
{
    /// <summary>
    /// Test board layouts for debugging irregular shapes in Editor.
    /// Not included in release builds.
    /// </summary>
    public static class BoardTestLayouts
    {
        public static bool[,] Get(int shape, int rows, int cols)
        {
            var layout = new bool[rows, cols];

            switch (shape)
            {
                case 1: // L-shape
                    for (int r = 0; r < rows; r++)
                        for (int c = 0; c < cols; c++)
                            layout[r, c] = c < cols / 2 || r >= rows / 2;
                    break;

                case 2: // Cross / plus
                    for (int r = 0; r < rows; r++)
                        for (int c = 0; c < cols; c++)
                            layout[r, c] = (r >= 2 && r < rows - 2) || (c >= 2 && c < cols - 2);
                    break;

                case 3: // Diamond
                    int cx = cols / 2, cy = rows / 2;
                    int radius = System.Math.Min(cx, cy);
                    for (int r = 0; r < rows; r++)
                        for (int c = 0; c < cols; c++)
                            layout[r, c] = System.Math.Abs(r - cy) + System.Math.Abs(c - cx) <= radius;
                    break;

                case 4: // U-shape
                    for (int r = 0; r < rows; r++)
                        for (int c = 0; c < cols; c++)
                            layout[r, c] = r >= rows / 2 || c < 2 || c >= cols - 2;
                    break;

                case 5: // Donut (rect with hole)
                    for (int r = 0; r < rows; r++)
                        for (int c = 0; c < cols; c++)
                            layout[r, c] = r < 2 || r >= rows - 2 || c < 2 || c >= cols - 2;
                    break;

                case 6: // Single hole in center
                    for (int r = 0; r < rows; r++)
                        for (int c = 0; c < cols; c++)
                            layout[r, c] = !(r == rows / 2 && c == cols / 2);
                    break;

                case 7: // 2x2 hole in center
                    for (int r = 0; r < rows; r++)
                        for (int c = 0; c < cols; c++)
                            layout[r, c] = !((r == rows / 2 || r == rows / 2 - 1) &&
                                             (c == cols / 2 || c == cols / 2 - 1));
                    break;

                case 8: // Single row (height=1)
                    layout = new bool[1, cols];
                    for (int c = 0; c < cols; c++)
                        layout[0, c] = true;
                    break;

                case 9: // Single column (width=1)
                    layout = new bool[rows, 1];
                    for (int r = 0; r < rows; r++)
                        layout[r, 0] = true;
                    break;

                default: // 0 = full rectangle
                    for (int r = 0; r < rows; r++)
                        for (int c = 0; c < cols; c++)
                            layout[r, c] = true;
                    break;
            }

            return layout;
        }
    }
}
#endif
