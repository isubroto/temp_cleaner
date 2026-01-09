using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace TempCleaner
{
    public static class CustomMessageBox
    {
        public enum MessageBoxButton
        {
            OK,
            OKCancel,
            YesNo,
            YesNoCancel
        }

        public enum MessageBoxImage
        {
            None,
            Information,
            Warning,
            Error,
            Question
        }

        public enum MessageBoxResult
        {
            None,
            OK,
            Cancel,
            Yes,
            No
        }

        public static MessageBoxResult Show(Window owner, string message, string title, 
            MessageBoxButton buttons = MessageBoxButton.OK, 
            MessageBoxImage icon = MessageBoxImage.None)
        {
            var dialog = new Window
            {
                Title = title,
                Width = 480,
                Height = 220,
                WindowStyle = WindowStyle.None,
                ResizeMode = ResizeMode.NoResize,
                WindowStartupLocation = owner?.IsLoaded == true && !owner.IsClosing() 
                    ? WindowStartupLocation.CenterOwner 
                    : WindowStartupLocation.CenterScreen,
                Background = Brushes.Transparent,
                AllowsTransparency = true
            };

            // Only set owner if it's valid and not closed/closing
            if (owner?.IsLoaded == true && !owner.IsClosing())
            {
                try
                {
                    dialog.Owner = owner;
                }
                catch
                {
                    // If setting owner fails, dialog will still display centered on screen
                }
            }

            var result = MessageBoxResult.None;

            // Get resources from owner if available, otherwise use Application resources
            var resourceSource = (owner?.IsLoaded == true ? (FrameworkElement)owner : Application.Current.MainWindow) 
                                 ?? Application.Current.MainWindow;

            // Main border with deep sea theme
            var border = new Border
            {
                CornerRadius = new CornerRadius(20),
                Background = (Brush)GetResource(resourceSource, "AbyssSurface"),
                BorderBrush = GetBorderBrush(resourceSource, icon),
                BorderThickness = new Thickness(2),
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    Color = GetGlowColor(icon),
                    Direction = 270,
                    ShadowDepth = 0,
                    Opacity = 0.4,
                    BlurRadius = 30
                },
                Padding = new Thickness(32, 24, 32, 24)
            };

            var mainStack = new StackPanel();

            // Title bar
            var titleBar = new Grid
            {
                Margin = new Thickness(0, 0, 0, 20)
            };
            titleBar.ColumnDefinitions.Add(new ColumnDefinition());
            titleBar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var titleStack = new StackPanel { Orientation = Orientation.Horizontal };
            
            // Icon
            if (icon != MessageBoxImage.None)
            {
                var iconBorder = new Border
                {
                    Width = 32,
                    Height = 32,
                    CornerRadius = new CornerRadius(16),
                    Background = GetIconBackground(icon),
                    Margin = new Thickness(0, 0, 12, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };

                var iconText = new TextBlock
                {
                    Text = GetIconText(icon),
                    FontSize = 16,
                    FontWeight = FontWeights.Bold,
                    Foreground = Brushes.White,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };

                iconBorder.Child = iconText;
                titleStack.Children.Add(iconBorder);
            }

            // Title
            var titleBlock = new TextBlock
            {
                Text = title,
                Foreground = (Brush)GetResource(resourceSource, "TextPrimary"),
                FontSize = 18,
                FontFamily = new FontFamily("Segoe UI Variable, Inter, Segoe UI"),
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center
            };
            titleStack.Children.Add(titleBlock);
            Grid.SetColumn(titleStack, 0);
            titleBar.Children.Add(titleStack);

            mainStack.Children.Add(titleBar);

            // Message
            var messageBlock = new TextBlock
            {
                Text = message,
                Foreground = (Brush)GetResource(resourceSource, "TextSecondary"),
                FontSize = 14,
                FontFamily = new FontFamily("Segoe UI Variable, Inter, Segoe UI"),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 24),
                LineHeight = 22
            };
            mainStack.Children.Add(messageBlock);

            // Buttons
            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };

            switch (buttons)
            {
                case MessageBoxButton.OK:
                    AddButton(buttonPanel, resourceSource, "OK", true, () => { result = MessageBoxResult.OK; dialog.Close(); });
                    break;

                case MessageBoxButton.OKCancel:
                    AddButton(buttonPanel, resourceSource, "Cancel", false, () => { result = MessageBoxResult.Cancel; dialog.Close(); });
                    AddButton(buttonPanel, resourceSource, "OK", true, () => { result = MessageBoxResult.OK; dialog.Close(); });
                    break;

                case MessageBoxButton.YesNo:
                    AddButton(buttonPanel, resourceSource, "No", false, () => { result = MessageBoxResult.No; dialog.Close(); });
                    AddButton(buttonPanel, resourceSource, "Yes", true, () => { result = MessageBoxResult.Yes; dialog.Close(); });
                    break;

                case MessageBoxButton.YesNoCancel:
                    AddButton(buttonPanel, resourceSource, "Cancel", false, () => { result = MessageBoxResult.Cancel; dialog.Close(); });
                    AddButton(buttonPanel, resourceSource, "No", false, () => { result = MessageBoxResult.No; dialog.Close(); });
                    AddButton(buttonPanel, resourceSource, "Yes", true, () => { result = MessageBoxResult.Yes; dialog.Close(); });
                    break;
            }

            mainStack.Children.Add(buttonPanel);

            border.Child = mainStack;
            dialog.Content = border;

            // Allow Escape to close
            dialog.KeyDown += (s, e) =>
            {
                if (e.Key == System.Windows.Input.Key.Escape)
                {
                    result = MessageBoxResult.Cancel;
                    dialog.Close();
                }
            };

            dialog.ShowDialog();
            return result;
        }

        private static object GetResource(FrameworkElement source, string key)
        {
            try
            {
                return source?.FindResource(key) ?? Application.Current.FindResource(key);
            }
            catch
            {
                // Fallback to default brush if resource not found
                return key switch
                {
                    "AbyssSurface" => new SolidColorBrush(Color.FromRgb(15, 23, 42)),
                    "TextPrimary" => Brushes.White,
                    "TextSecondary" => new SolidColorBrush(Color.FromRgb(203, 213, 225)),
                    "AbyssCard" => new SolidColorBrush(Color.FromRgb(30, 41, 59)),
                    "BioTeal" => new SolidColorBrush(Color.FromRgb(0, 212, 170)),
                    "BioBlue" => new SolidColorBrush(Color.FromRgb(0, 153, 255)),
                    "BioCyan" => new SolidColorBrush(Color.FromRgb(0, 229, 255)),
                    "DangerGlow" => new SolidColorBrush(Color.FromRgb(255, 107, 107)),
                    "WarningGlow" => new SolidColorBrush(Color.FromRgb(255, 179, 71)),
                    _ => Brushes.Gray
                };
            }
        }

        private static bool IsClosing(this Window window)
        {
            try
            {
                // Check if window is in a closing or closed state
                return window == null || !window.IsLoaded || 
                       PresentationSource.FromVisual(window) == null;
            }
            catch
            {
                return true;
            }
        }

        private static void AddButton(StackPanel panel, FrameworkElement resourceSource, string text, bool isPrimary, Action onClick)
        {
            var button = new Button
            {
                Content = text,
                Height = 38,
                MinWidth = 100,
                Margin = new Thickness(8, 0, 0, 0),
                FontSize = 13,
                FontFamily = new FontFamily("Segoe UI Variable, Inter, Segoe UI"),
                FontWeight = FontWeights.Medium,
                Cursor = System.Windows.Input.Cursors.Hand
            };

            if (isPrimary)
            {
                try
                {
                    button.Style = (Style)GetResource(resourceSource, "AbyssButton");
                }
                catch
                {
                    // Fallback styling if resource not found
                    button.Background = new SolidColorBrush(Color.FromRgb(0, 212, 170));
                    button.Foreground = Brushes.White;
                    button.BorderThickness = new Thickness(0);
                }
            }
            else
            {
                button.Background = (Brush)GetResource(resourceSource, "AbyssCard");
                button.Foreground = (Brush)GetResource(resourceSource, "TextSecondary");
                button.BorderThickness = new Thickness(1);
                button.BorderBrush = (Brush)GetResource(resourceSource, "BioTeal");
                
                button.Template = new ControlTemplate(typeof(Button))
                {
                    VisualTree = CreateButtonTemplate(resourceSource)
                };
            }

            button.Click += (s, e) => onClick();
            panel.Children.Add(button);
        }

        private static FrameworkElementFactory CreateButtonTemplate(FrameworkElement resourceSource)
        {
            var border = new FrameworkElementFactory(typeof(Border));
            border.Name = "border";
            border.SetValue(Border.BackgroundProperty, GetResource(resourceSource, "AbyssCard"));
            border.SetValue(Border.BorderBrushProperty, GetResource(resourceSource, "BioTeal"));
            border.SetValue(Border.BorderThicknessProperty, new Thickness(1));
            border.SetValue(Border.CornerRadiusProperty, new CornerRadius(10));
            border.SetValue(Border.PaddingProperty, new Thickness(20, 8, 20, 8));

            var contentPresenter = new FrameworkElementFactory(typeof(ContentPresenter));
            contentPresenter.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            contentPresenter.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            
            border.AppendChild(contentPresenter);

            return border;
        }

        private static Brush GetBorderBrush(FrameworkElement resourceSource, MessageBoxImage icon)
        {
            return icon switch
            {
                MessageBoxImage.Error => (Brush)GetResource(resourceSource, "DangerGlow"),
                MessageBoxImage.Warning => (Brush)GetResource(resourceSource, "WarningGlow"),
                MessageBoxImage.Information => (Brush)GetResource(resourceSource, "BioBlue"),
                MessageBoxImage.Question => (Brush)GetResource(resourceSource, "BioCyan"),
                _ => (Brush)GetResource(resourceSource, "BioTeal")
            };
        }

        private static Brush GetIconBackground(MessageBoxImage icon)
        {
            return icon switch
            {
                MessageBoxImage.Error => new SolidColorBrush(Color.FromRgb(220, 53, 69)),      // Red
                MessageBoxImage.Warning => new SolidColorBrush(Color.FromRgb(255, 193, 7)),    // Amber/Orange
                MessageBoxImage.Information => new SolidColorBrush(Color.FromRgb(13, 110, 253)), // Blue
                MessageBoxImage.Question => new SolidColorBrush(Color.FromRgb(13, 202, 240)),  // Cyan
                _ => new SolidColorBrush(Color.FromRgb(0, 212, 170))                           // Teal
            };
        }

        private static Color GetGlowColor(MessageBoxImage icon)
        {
            return icon switch
            {
                MessageBoxImage.Error => Color.FromRgb(255, 107, 107),
                MessageBoxImage.Warning => Color.FromRgb(255, 179, 71),
                MessageBoxImage.Information => Color.FromRgb(0, 153, 255),
                MessageBoxImage.Question => Color.FromRgb(0, 229, 255),
                _ => Color.FromRgb(0, 212, 170)
            };
        }

        private static string GetIconText(MessageBoxImage icon)
        {
            return icon switch
            {
                MessageBoxImage.Error => "?",
                MessageBoxImage.Warning => "!",
                MessageBoxImage.Information => "i",
                MessageBoxImage.Question => "?",
                _ => "•"
            };
        }
    }
}
