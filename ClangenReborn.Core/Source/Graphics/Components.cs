using Assimp;
using ClangenReborn.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace ClangenReborn.Components;

public abstract class ComponentBase()
{
    public bool IsVisible = true;
    public bool IsEnabled = true;

    public Rectangle Bounds;
    public Color Color = Color.White;

    public Action<MouseState>? OnClick;
    public Action<MouseState>? OnHover;

    public abstract void Draw(SpriteBatchEx Batch);
    public virtual void DrawHovered(SpriteBatchEx Batch) => Draw(Batch);
    public virtual void DrawUnavailable(SpriteBatchEx Batch) => Draw(Batch);

    public virtual bool Contains(Point Position) => this.Bounds.Contains(Position);
}

public abstract class TickingComponentBase : ComponentBase
{
    public abstract void Update(GameTime GameTime);
}

public class ComponentSet<TComponent> : TickingComponentBase, IEnumerable<TComponent>, ICollection<TComponent> where TComponent : ComponentBase // OPTIMIZE
{
    private readonly HashSet<TComponent> Components;
    private readonly HashSet<TComponent> DisabledComponents;
    private readonly HashSet<TComponent> HoveredComponents;
    private HashSet<TComponent>? PotentialClicks = null;
    private MouseButton PotentialClicksType;

    public Action<SpriteBatchEx>? Begin;
    public Action<SpriteBatchEx>? BeginHovered;
    public Action<SpriteBatchEx>? BeginUnavailable;

    public int Count => this.Components.Count + this.DisabledComponents.Count + this.HoveredComponents.Count;
    public bool IsReadOnly => false;

    public ComponentSet(IEnumerable<TComponent> Components)
    {
        this.Components = new (Components);
        this.DisabledComponents = [];
        this.HoveredComponents = [];
        this.PotentialClicks = [];
    }
    public ComponentSet(params TComponent[] Components) : this(Components.AsEnumerable()) { }

    public override void Update(GameTime GameTime)
    {
        MouseState CurrentMouseState = Mouse.GetState();

        if (this.PotentialClicks is not null)
        {
            if (this.PotentialClicksType is MouseButton.Left && CurrentMouseState.LeftButton is ButtonState.Released)
            {
                foreach (var C in this.PotentialClicks) C.OnClick?.Invoke(CurrentMouseState);
                this.PotentialClicks = null;
            }
            else if (this.PotentialClicksType is MouseButton.Right && CurrentMouseState.RightButton is ButtonState.Released)
            {
                foreach (var C in this.PotentialClicks) C.OnClick?.Invoke(CurrentMouseState);
                this.PotentialClicks = null;
            }
            else if (this.PotentialClicksType is MouseButton.Middle && CurrentMouseState.MiddleButton is ButtonState.Released)
            {
                foreach (var C in this.PotentialClicks) C.OnClick?.Invoke(CurrentMouseState);
                this.PotentialClicks = null;
            }
        }
        else
        {
            if (CurrentMouseState.LeftButton is ButtonState.Pressed)
            {
                this.PotentialClicks = this.HoveredComponents;
                this.PotentialClicksType = MouseButton.Left;
            }
            else if (CurrentMouseState.RightButton is ButtonState.Pressed)
            {
                this.PotentialClicks = this.HoveredComponents;
                this.PotentialClicksType = MouseButton.Right;
            }
            else if (CurrentMouseState.MiddleButton is ButtonState.Pressed)
            {
                this.PotentialClicks = this.HoveredComponents;
                this.PotentialClicksType = MouseButton.Middle;
            }
        }

        foreach (TComponent Component in this.DisabledComponents)
        {
            if (Component.IsEnabled)
            {
                this.Components.Add(Component);
                this.DisabledComponents.Remove(Component);
            }
        }

        foreach (TComponent Component in this.HoveredComponents)
        {
            if (!Component.IsEnabled || !Component.IsVisible || !Component.Contains(CurrentMouseState.Position))
            {
                this.HoveredComponents.Remove(Component);
                this.Components.Add(Component);
            }
            else if (!Component.IsEnabled)
            {
                this.DisabledComponents.Add(Component);
                this.Components.Remove(Component);
            }
        }
        foreach (TComponent Component in this.Components)
        {
            if (Component.IsEnabled && Component.IsVisible && Component.Contains(CurrentMouseState.Position))
            {
                this.HoveredComponents.Add(Component);
                this.Components.Remove(Component);
            }
            else if (!Component.IsEnabled)
            {
                this.DisabledComponents.Add(Component);
                this.Components.Remove(Component);
            }
        }
       
    }

    public override void Draw(SpriteBatchEx Batch)
    {
        this.Begin?.Invoke(Batch);
        if (!Batch.BeginCalled)
            Batch.Begin();

        foreach (TComponent Component in this.Components)
            Component.Draw(Batch);

        if (0 < this.DisabledComponents.Count)
        {
            if (this.BeginUnavailable is not null)
            {
                Batch.End();
                this.BeginUnavailable.Invoke(Batch);
                if (!Batch.BeginCalled)
                    Batch.Begin();
            }
            else if (this.Begin is not null)
            {
                Batch.End();
                Batch.Begin();
            }

            foreach (TComponent Component in this.DisabledComponents)
                Component.Draw(Batch);
        }

        if (0 < this.HoveredComponents.Count)
        {
            if (this.BeginHovered is not null)
            {
                Batch.End();
                this.BeginHovered.Invoke(Batch);
                if (!Batch.BeginCalled)
                    Batch.Begin();
            }
            else if (this.BeginUnavailable is not null)
            {
                Batch.End();
                Batch.Begin();
            }

            foreach (TComponent Component in this.HoveredComponents)
                Component.Draw(Batch);
        }

        Batch.End();
    }

    public IEnumerator<TComponent> GetEnumerator() => this.Components.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public void Add(TComponent Item)
    {
        this.Components.Add(Item);
    }

    public void Clear()
    {
        this.Components.Clear();
        this.HoveredComponents.Clear();
        this.DisabledComponents.Clear();
    }

    public bool Contains(TComponent Item) => this.Components.Contains(Item) || this.HoveredComponents.Contains(Item) || this.DisabledComponents.Contains(Item);

    public void CopyTo(TComponent[] Array, int ArrayIndex)
    {
        this.Components.CopyTo(Array, ArrayIndex);
        ArrayIndex += this.Components.Count;

        if (ArrayIndex >= this.Count) return;
        this.HoveredComponents.CopyTo(Array, ArrayIndex);
        ArrayIndex += this.HoveredComponents.Count;

        if (ArrayIndex >= this.Count) return;
        this.DisabledComponents.CopyTo(Array, ArrayIndex);
    }

    public bool Remove(TComponent Item) => this.Components.Remove(Item) || this.HoveredComponents.Remove(Item) || this.DisabledComponents.Remove(Item);
}

public class Button : ComponentBase
{
    public string Texture;
    public string? TextureHovered;
    public string? TextureUnavailable;

    public Button(Rectangle Bounds, string Texture)
    {
        this.Bounds = Bounds;
        this.Texture = Texture;
    }

    public override void Draw(SpriteBatchEx Batch)
    {
        Content.DrawTexture(this.Texture, this.Bounds, this.Color);
    }

    public override void DrawHovered(SpriteBatchEx Batch)
    {
        Content.DrawTexture(this.TextureHovered ?? this.Texture, this.Bounds, this.Color);
    }

    public override void DrawUnavailable(SpriteBatchEx Batch)
    {
        Content.DrawTexture(this.TextureUnavailable ?? this.Texture, this.Bounds, this.Color);
    }
}

public class ButtonWithText : Button
{
    public string Text;
    public string Font;
    public Color FontColor = Color.White;

    public ButtonWithText(Rectangle Bounds, string Texture, string Text, string Font) : base(Bounds, Texture)
    {
        this.Texture = Texture;
        this.Text = Text;
        this.Font = Font;
    }

    public override void Draw(SpriteBatchEx Batch)
    {
        base.Draw(Batch);
        Content.DrawFont(this.Font, this.Text, this.Bounds, this.FontColor, 20);
    }

    public override void DrawHovered(SpriteBatchEx Batch)
    {
        base.DrawHovered(Batch);
        Content.DrawFont(this.Font, this.Text, this.Bounds, this.FontColor, 20);
    }

    public override void DrawUnavailable(SpriteBatchEx Batch)
    {
        base.DrawUnavailable(Batch);
        Content.DrawFont(this.Font, this.Text, this.Bounds, this.FontColor, 20);
    }
}

public class Slider : ComponentBase
{
    public string Texture;

    public int Min;
    public int Max;

    private int _Value;
    public int Value
    {
        get => this._Value; protected set => this._Value = int.Clamp(value, this.Min, this.Max);
    }

    public double RelativeValue => double.CreateChecked((this._Value - this.Min) / (this.Max - this.Min));

    public Slider()
    {
        this.Min = int.MinValue;
        this.Max = int.MaxValue;
        this.Value = 0;
    }
    public Slider(Rectangle Bounds, string Texture) : this(Bounds, Texture, int.MinValue, int.MaxValue) { }
    public Slider(Rectangle Bounds, string Texture, int Min, int Max) : this(Bounds, Texture, Min, Max, Min) { }
    public Slider(Rectangle Bounds, string Texture, int Min, int Max, int Default)
    {
        this.Bounds = Bounds;
        this.Texture = Texture;
        this.Min = Min;
        this.Max = Max;
        this._Value = Default;
    }

    public override void Draw(SpriteBatchEx Batch)
    {

    }
}

public class Slider<T> : Slider where T : notnull, IComparable<T>, INumber<T>, IMinMaxValue<T>
{
    public new T Min;
    public new T Max;

    private T _Value;
    public new T Value 
    {
        get => this._Value; protected set => this._Value = T.Clamp(value, this.Min, this.Max);
    }

    public Slider()
    {
        this.Min = T.MinValue;
        this.Max = T.MaxValue;
        this.Value = T.Zero;
    }
    public Slider(Rectangle Bounds, string Texture) : this(Bounds, Texture, T.MinValue, T.MaxValue) { }
    public Slider(Rectangle Bounds, string Texture, T Min, T Max) : this(Bounds, Texture, Min, Max, Min) { }
    public Slider(Rectangle Bounds, string Texture, T Min, T Max, T Default)
    {
        this.Bounds = Bounds;
        this.Texture = Texture;
        this.Min = Min;
        this.Max = Max;
        this._Value = Default;
    }
}


public class DynamicNinePatchBody : ComponentBase
{
    public string Texture;
    public int Left;
    public int Top;
    public int Right;
    public int Bottom;

    public bool BoundsRepresentCentre = false;

    public DynamicNinePatchBody(Rectangle Bounds, string Texture, int Left, int Top, int Right, int Bottom)
    {
        this.Bounds = Bounds;
        this.Texture = Texture;

        this.Left = Left;
        this.Top = Top;
        this.Right = Right;
        this.Bottom = Bottom;

        SetBounds(Bounds);
    }


    public DynamicNinePatchBody(Rectangle Bounds, string Texture, int Left, int Top, int Right, int Bottom, bool BoundsRepresentCentre)
    {
        this.Bounds = Bounds;
        this.Texture = Texture;

        this.Left = Left;
        this.Top = Top;
        this.Right = Right;
        this.Bottom = Bottom;

        this.BoundsRepresentCentre = BoundsRepresentCentre;

        SetBounds(Bounds);
    }

    public void SetPosition(int X, int Y)
    {
        if (this.BoundsRepresentCentre)
        {
            this.Bounds.X = X - this.Left;
            this.Bounds.Y = Y - this.Top;
        }
        else
        {
            this.Bounds.X = X;
            this.Bounds.Y = Y;
        }
    }

    public void SetBounds(Rectangle Bounds) => SetBounds(Bounds.X, Bounds.Y, Bounds.Width, Bounds.Height);
    public void SetBounds(int X, int Y, int Width, int Height)
    {
        if (this.BoundsRepresentCentre)
        {
            this.Bounds.X = X - this.Left;
            this.Bounds.Y = Y - this.Top;
            this.Bounds.Width = Width + this.Left.Below(this.Bounds.Width) * 2;
            this.Bounds.Height = Height + this.Top.Below(this.Bounds.Height) * 2;
        }
        else
        {
            this.Bounds.X = X;
            this.Bounds.Y = Y;
            this.Bounds.Width = Width;
            this.Bounds.Height = Height;
        }
    }

    public override void Draw(SpriteBatchEx Batch) 
        // TODO Allow for the center box to be defined as the bounds, meaning the borders will be drawn around them
        // + Optimise with the ability to create one static texture to avoid 9 draw calls 
        // + I made this one night since the original was VERY slow and in truth I have no idea what I did
    {
        int DestWidth, DestHeight;
        int CornerWidth = this.Left.Below(this.Bounds.Width);
        int CornerHeight = this.Top.Below(this.Bounds.Height);

        // Centre
        if (this.Left < this.Bounds.Width && this.Top < this.Bounds.Height)
            Content.DrawTexture(this.Texture, 
                new(this.Bounds.X + this.Left, this.Bounds.Y + this.Top, DestWidth = (this.Right - this.Left).Below(this.Bounds.Width - (this.Left << 1)), DestHeight = (this.Bottom - this.Top).Below(this.Bounds.Height - (this.Top << 1))), 
                new(this.Left, this.Top, DestWidth, DestHeight),
                this.Color
            );

        // Corners
        Content.DrawTexture(this.Texture, 
            new(this.Bounds.X, this.Bounds.Y, CornerWidth, CornerHeight), 
            new(0, 0, CornerWidth, CornerHeight), 
            this.Color
        );
        Content.DrawTexture(this.Texture,
            new(this.Bounds.X + this.Bounds.Width - this.Left, this.Bounds.Y, CornerWidth, CornerHeight),
            new(this.Right, 0, CornerWidth, CornerHeight),  this.Color
        );

        Content.DrawTexture(this.Texture,
            new(this.Bounds.X, this.Bounds.Y + this.Bounds.Height - this.Top, CornerWidth, CornerHeight),
            new(0, this.Bottom, CornerWidth, CornerHeight),
            this.Color
        );
        Content.DrawTexture(this.Texture,
            new(this.Bounds.X + this.Bounds.Width - this.Left, this.Bounds.Y + this.Bounds.Height - this.Top, CornerWidth, CornerHeight), 
            new(this.Right, this.Bottom, CornerWidth, CornerHeight),
            this.Color
        );

        // Sides
        Content.DrawTexture(this.Texture,
            new(this.Bounds.X + this.Left, this.Bounds.Y, DestWidth = (this.Right - this.Left).Below(this.Bounds.Width - (this.Left << 1)), DestHeight = this.Top.Below(this.Bounds.Height)),
            new(this.Left, 0, DestWidth, DestHeight),
            this.Color
        );
        Content.DrawTexture(this.Texture,
            new(this.Bounds.X, this.Bounds.Y + this.Top, DestWidth = this.Left.Below(this.Bounds.Width), DestHeight = (this.Bottom - this.Top).Below(this.Bounds.Height - (this.Top << 1))),
            new(0, this.Top, DestWidth, DestHeight),
            this.Color
        );
        Content.DrawTexture(this.Texture,
            new(this.Bounds.X + this.Left, this.Bounds.Y + this.Bounds.Height - this.Top, DestWidth = (this.Right - this.Left).Below(this.Bounds.Width - (this.Left << 1)), DestHeight = this.Top.Below(this.Bounds.Height)),
            new(this.Left, this.Bottom, DestWidth, DestHeight), 
            this.Color
        );
        Content.DrawTexture(this.Texture,
            new(this.Bounds.X + this.Bounds.Width - this.Left, this.Bounds.Y + this.Top, DestWidth = this.Left.Below(this.Bounds.Width), DestHeight = (this.Bottom - this.Top).Below(this.Bounds.Height - (this.Top << 1))),
            new(this.Right, this.Top, DestWidth, DestHeight), 
            this.Color
        );
    }
}