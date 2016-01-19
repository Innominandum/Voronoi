using System;
using System.IO;
using System.Windows.Forms;

namespace Voronoi.UI
{
    public partial class Form1 : Form
    {
        private string _initialDirectory = string.Empty;

        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                int intSeed = Convert.ToInt32(DateTime.Now.Ticks & 0x0000FFFF);

                var currentMap = new Map()
                {
                    Height = 1200,
                    Width = 1920,
                    NumberOfSites = 4000,
                    LloydIterations = 2,
                    PolygonSeed = intSeed,
                    PerlinSeed = intSeed,
                    ShowBorders = true,
                    MapType = Enumerations.MapType.Elevation,
                    NoiseOctaves = 8,
                    IslandShape = Enumerations.MapShape.Perlin
                };

                currentMap.Create();
                
                string strRocka = currentMap.VectorMap;

                var dialog = new SaveFileDialog()
                {
                    AddExtension = true,
                    CheckPathExists = true,
                    DefaultExt = ".svg",
                    Filter = "SVG (*.svg)|*.svg",
                    FileName = $"{intSeed}.svg"
                };

                if (!string.IsNullOrEmpty(_initialDirectory))
                {
                    dialog.InitialDirectory = _initialDirectory;
                }

                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                File.WriteAllText(dialog.FileName, strRocka);

                _initialDirectory = Path.GetDirectoryName(dialog.FileName);

                MessageBox.Show($"Done! {currentMap.Debug.ToString()}", "Voronoi", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
