using System.Drawing;
using System.Windows.Forms;
using FormWind.Krypton;
using FormWind.Tests.Performance;
using Krypton.Toolkit;
using Xunit;

namespace FormWind.Tests;

[Collection(UiCollection.Name)]
public class KryptonTests
{
    static KryptonTests() => KryptonApplier.Register();

    private static int Argb(string hex) => ColorTranslator.FromHtml(hex).ToArgb();

    // Krypton başlıkları ve hücreleri kendi paletiyle çizer; DataGridView hücre stillerinin zeminini yok sayardı.
    [Fact]
    public void Krypton_grid_headers_and_cells_are_painted_with_class_colors() => Sta.Run(() =>
    {
        using var provider = new TailwindProvider { DarkMode = true };
        using var form = new Form { StartPosition = FormStartPosition.Manual, Location = new Point(-10000, -10000), ShowInTaskbar = false, Size = new Size(400, 300) };
        var grid = new KryptonDataGridView { Dock = DockStyle.Fill, AllowUserToAddRows = false, RowHeadersVisible = false };
        grid.Columns.Add("name", "Ad");
        grid.Rows.Add("Ayşe");
        grid.ClearSelection();
        form.Controls.Add(grid);
        provider.SetClasses(grid, "bg-white text-gray-700 header:bg-gray-50 header:text-gray-500 "
                                  + "dark:bg-gray-800 dark:text-gray-200 dark:header:bg-gray-900 dark:header:text-gray-400");
        form.Show();
        grid.ClearSelection();

        using var bitmap = new Bitmap(grid.Width, grid.Height);
        grid.DrawToBitmap(bitmap, new Rectangle(Point.Empty, grid.Size));
        var headerCell = grid.GetCellDisplayRectangle(0, -1, false);
        var dataCell = grid.GetCellDisplayRectangle(0, 0, false);
        // Yazıdan uzak, hücrenin sağ kenarına yakın bir piksel.
        Assert.Equal(Argb("#111827"), bitmap.GetPixel(headerCell.Right - 4, headerCell.Top + headerCell.Height / 2).ToArgb());
        Assert.Equal(Argb("#1f2937"), bitmap.GetPixel(dataCell.Right - 4, dataCell.Top + dataCell.Height / 2).ToArgb());

        Assert.Equal(Argb("#9ca3af"), grid.StateCommon.HeaderColumn.Content.Color1.ToArgb());
        provider.DarkMode = false;
        Assert.Equal(Argb("#f9fafb"), grid.StateCommon.HeaderColumn.Back.Color1.ToArgb());
        Assert.Equal(Color.White.ToArgb(), grid.StateCommon.DataCell.Back.Color1.ToArgb());

        provider.SetClasses(grid, "");
        Assert.Equal(PaletteColorStyle.Inherit, grid.StateCommon.HeaderColumn.Back.ColorStyle);
        form.Close();
    });
}
