using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Runtime.CompilerServices;


namespace ClangenReborn.Graphics;

public class SpriteBatchEx : SpriteBatch
{
    private readonly Texture2D _Pixel;

    public bool BeginCalled { get; private set; }

    public SpriteBatchEx(GraphicsDevice GraphicsDevice) : base(GraphicsDevice)
    {
        this._Pixel = new Texture2D(GraphicsDevice, 1, 1, false, SurfaceFormat.Color);
        this._Pixel.SetData([Color.White]);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public new void Begin(SpriteSortMode sortMode = SpriteSortMode.Deferred, BlendState? blendState = null, SamplerState? samplerState = null, DepthStencilState? depthStencilState = null, RasterizerState? rasterizerState = null, Effect? effect = null, Matrix? transformMatrix = null)
    {
        base.Begin(sortMode, blendState, samplerState, depthStencilState, rasterizerState, effect, transformMatrix);
        this.BeginCalled = true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public new void End()
    {
        base.End();
        this.BeginCalled = false;
    }

    public void DrawLine(Vector2 Origin, float Length, float Angle, Color Color, float Thickness)
        => Draw(this._Pixel, Origin, null, Color, Angle, Vector2.Zero, new Vector2(Length, Thickness), SpriteEffects.None, 0);
    public void DrawLine(Vector2 Origin, Vector2 End, Color Color, float Thickness)
        => DrawLine(Origin, Vector2.Distance(Origin, End), (float)Math.Atan2(End.Y - Origin.Y, End.X - Origin.X), Color, Thickness);

    public void DrawRectangle(Rectangle Rectangle, Color Color)
        => Draw(this._Pixel, Rectangle, Color);
    public void DrawRectangle(Rectangle Rectangle, Color Color, float Angle)
        => Draw(this._Pixel, Rectangle, null, Color, Angle, Vector2.Zero, SpriteEffects.None, 0);
    public void DrawRectangle(Vector2 Origin, Vector2 Size, Color Color, float Angle)
        => Draw(this._Pixel, Origin, null, Color, Angle, Vector2.Zero, Size, SpriteEffects.None, 0);
}