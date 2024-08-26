using FontStashSharp;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;

namespace ClangenReborn.Graphics;

public class Font 
    // TODO Polish, need I say more?
{
    private readonly FontSystem Collector;
    public readonly DynamicSpriteFont Face;
    public readonly string Path;


    public Font(string Path, uint Unit) 
    {
        this.Path = Path;

        this.Collector = new ();

        using (FileStream FontStream = File.OpenRead(Path))
        {
            this.Collector.AddFont(FontStream);
        }

        this.Face = this.Collector.GetFont(Unit);
    }

    public void Draw(SpriteBatchEx Batch, string Text, Rectangle Bounds, Color? Color = null)
    {
        Vector2 ActualSize = this.Face.MeasureString(Text);

        this.Face.DrawText(
            Batch, Text, Bounds.Center.ToVector2(), Color ?? Microsoft.Xna.Framework.Color.White, 0, ActualSize / 2, new Vector2(0.5f, 0.5f), 0, 0, 0, TextStyle.None, FontSystemEffect.None, 0
        );
    }
}

