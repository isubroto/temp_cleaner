using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace TempCleaner
{
    public static class UpdateDialogHelper
    {
        public static void Show(Window owner, string currentVersion, string latestVersion, string downloadUrl, string fileName, string token)
        {
            var updateDialog = new Window
            {
                Title = "Update Available",
                Width = 450,
                Height = 350,
                WindowStyle = WindowStyle.None,
                ResizeMode = ResizeMode.NoResize,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = owner,
                Background = Brushes.Transparent,
                AllowsTransparency = true
            };

            var border = new Border
            {
                CornerRadius = new CornerRadius(20),
                Background = (Brush)owner.FindResource("AbyssSurface"),
                BorderBrush = (Brush)owner.FindResource("BioTeal"),
                BorderThickness = new Thickness(2),
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    Color = Color.FromRgb(0, 212, 170),
                    Direction = 270,
                    ShadowDepth = 0,
                    Opacity = 0.4,
                    BlurRadius = 30
                },
                Padding = new Thickness(40, 32, 40, 32)
            };

            var mainStack = new StackPanel();

            // Title
            mainStack.Children.Add(new TextBlock
            {
                Text = "DeepCleaner Update",
                Foreground = (Brush)owner.FindResource("TextPrimary"),
                FontSize = 20,
                FontFamily = new FontFamily("Segoe UI Variable, Inter, Segoe UI"),
                FontWeight = FontWeights.SemiBold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 8)
            });

            // Subtitle
            mainStack.Children.Add(new TextBlock
            {
                Text = "A new version is available!",
                Foreground = (Brush)owner.FindResource("TextSecondary"),
                FontSize = 13,
                FontFamily = new FontFamily("Segoe UI Variable, Inter, Segoe UI"),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 28)
            });

            // Version info
            var versionGrid = new Grid { Margin = new Thickness(0, 0, 0, 32) };
            versionGrid.ColumnDefinitions.Add(new ColumnDefinition());
            versionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(40) });
            versionGrid.ColumnDefinitions.Add(new ColumnDefinition());

            // Current version
            var currentCard = CreateVersionCard(owner, "Current", currentVersion);
            Grid.SetColumn(currentCard, 0);
            versionGrid.Children.Add(currentCard);

            // Arrow
            var arrow = new TextBlock
            {
                Text = "→",
                Foreground = (Brush)owner.FindResource("BioTeal"),
                FontSize = 24,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(arrow, 1);
            versionGrid.Children.Add(arrow);

            // Latest version
            var latestCard = CreateVersionCard(owner, "Latest", latestVersion);
            Grid.SetColumn(latestCard, 2);
            versionGrid.Children.Add(latestCard);

            mainStack.Children.Add(versionGrid);

            // Buttons
            var buttonGrid = new Grid();
            buttonGrid.ColumnDefinitions.Add(new ColumnDefinition());
            buttonGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(16) });
            buttonGrid.ColumnDefinitions.Add(new ColumnDefinition());
            
            var updateButton = new Button
            {
                Content = "Update",
                Height = 48,
                Style = (Style)owner.FindResource("AbyssButton"),
                FontSize = 15
            };
            updateButton.Click += (s, args) =>
            {
                updateDialog.Close();
                if (!string.IsNullOrEmpty(downloadUrl))
                {
                    var mainWindow = (MainWindow)owner;
                    var cleanName = mainWindow.GetType().GetMethod("CleanFileName", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)?
                        .Invoke(null, new object[] { fileName }) as string ?? fileName;
                    GitHubUpdater.DownloadUpdateWithProgress(downloadUrl, cleanName, token);
                }
            };
            Grid.SetColumn(updateButton, 0);
            buttonGrid.Children.Add(updateButton);

            var laterButton = new Button
            {
                Content = "Later",
                Height = 48,
                FontSize = 15,
                Style = new Style(typeof(Button), (Style)owner.FindResource("AbyssButton")),
                Background = (Brush)owner.FindResource("AbyssCard"),
                Foreground = (Brush)owner.FindResource("TextSecondary")
            };
            laterButton.Click += (s, args) => updateDialog.Close();
            Grid.SetColumn(laterButton, 2);
            buttonGrid.Children.Add(laterButton);

            mainStack.Children.Add(buttonGrid);

            border.Child = mainStack;
            updateDialog.Content = border;
            updateDialog.ShowDialog();
        }

        private static Border CreateVersionCard(Window owner, string label, string version)
        {
            var card = new Border
            {
                Background = (Brush)owner.FindResource("AbyssCard"),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(20, 16, 20, 16),
                BorderBrush = (Brush)owner.FindResource("BioTeal"),
                BorderThickness = new Thickness(1)
            };

            var stack = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
            
            stack.Children.Add(new TextBlock
            {
                Text = label,
                Foreground = (Brush)owner.FindResource("TextSecondary"),
                FontSize = 11,
                FontFamily = new FontFamily("Segoe UI Variable, Inter, Segoe UI"),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 8)
            });

            stack.Children.Add(new TextBlock
            {
                Text = $"v{version}",
                Foreground = (Brush)owner.FindResource("BioTeal"),
                FontSize = 20,
                FontFamily = new FontFamily("Segoe UI Variable, Inter, Segoe UI"),
                FontWeight = FontWeights.SemiBold,
                HorizontalAlignment = HorizontalAlignment.Center
            });

            card.Child = stack;
            return card;
        }
    }
}
