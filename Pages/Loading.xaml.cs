using System;
using System.Linq;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Platform;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Hosting;

namespace SnClient.Pages
{
    public partial class Loading : ContentPage
    {
        public Loading()
        {
            InitializeComponent();
            ApplyBlurEffect();
        }

        private void ApplyBlurEffect()
        {
            var handler = BackgroundImage.Handler as Image;
            var nativeImage = handler.ContainerElement;

            var compositor = ElementCompositionPreview.GetElementVisual(nativeImage).Compositor;
            var blurEffect = new GaussianBlurEffect
            {
                Source = new CompositionEffectSourceParameter("Backdrop"),
                BlurAmount = 10.0f,
                BorderMode = EffectBorderMode.Hard
            };

            var effectFactory = compositor.CreateEffectFactory(blurEffect);
            var backdropBrush = compositor.CreateBackdropBrush();
            var effectBrush = effectFactory.CreateBrush();
            effectBrush.SetSourceParameter("Backdrop", backdropBrush);

            var spriteVisual = compositor.CreateSpriteVisual();
            spriteVisual.Brush = effectBrush;
            spriteVisual.Size = new System.Numerics.Vector2((float)BackgroundImage.Width, (float)BackgroundImage.Height);

            ElementCompositionPreview.SetElementChildVisual(nativeImage, spriteVisual);
        }
    }
}