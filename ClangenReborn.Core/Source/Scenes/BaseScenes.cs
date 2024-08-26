using ClangenReborn.Components;
using ClangenReborn.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

using static ClangenReborn.Content;

namespace ClangenReborn.Scenes;

public class NameString(string Value) : CatName // TODO remove
{
    public override string GetName() => Value;
    public override bool IsValidName() => true;
}

public sealed class LoadingScreen : IScene // TODO Polish
{
    private static Texture2D? LoadingIcon;
    private static Thread? LoadingThread;
    private static bool IsFinalising = false;
    private static string Text = "Loading . . .";

    private static readonly Progress LoadingCat_LeaveTransition = new()
    {
        Duration = 3.0d, EaseIn = MathC.EaseInCubic, OnComplete = SetScene<Menu>
    };

    // TODO NOTE -> Animation will be abstracted, just here for now as testing
    private const int IconFrameMax = 10;
    private static int IconFrame = 0;
    private static double TotalSeconds = 0;

    private static Rectangle LoadingCat_Rect = new Rectangle(300, 250, 200, 200);

    public static void SetText(string Text)
    {
        LoadingScreen.Text = Text;
    }

    public LoadingScreen()
    {
        LoadingIcon = GetTexture("LoadingCat.png");
    }

    void IScene.Draw(SpriteBatchEx Batch)
    {
        Fill(Color.Black);

        Batch.Begin();
        Batch.Draw(
            LoadingIcon,
            LoadingCat_Rect,
            new Rectangle(200 * (IconFrame % 5), 200 * (IconFrame / 5), 200, 200), 
            Color.White
        );

        DrawFont("Clangen", Text, new Vector2(400, 500), Color.White);
        Batch.End();
    }



    void IScene.Update(GameTime GameTime)
    {
        if ((TotalSeconds += GameTime.ElapsedGameTime.TotalSeconds) > 0.1)
        {
            if (++IconFrame >= IconFrameMax)
                IconFrame = 0;

            TotalSeconds = 0;
        }

        if (LoadingThread is not null && LoadingThread.IsAlive)
            return;

        if (!IsFinalising)
        {
            LoadingThread = new Thread(FinaliseContext);
            LoadingThread.Start();
            IsFinalising = true;
            SetText("Finalising . . .");
            LoadingCat_LeaveTransition.Start();
        }

        if (!LoadingThread!.IsAlive)
        {
            LoadingCat_LeaveTransition.Update();
            LoadingCat_Rect.X = (int)(300 + (700 * LoadingCat_LeaveTransition.T));
        }
    }
    void IScene.Open()
    {
        LoadingThread = new Thread(CreateContext);
        LoadingThread.Start();
    }
}



[SceneInfo("Menu", true)]
public sealed class Menu : IScene
{
    private readonly ComponentSet<ButtonWithText> Buttons;
    private readonly Slider<int> Slider;

    private readonly Progress ContentPanel_OpenTransition;
    private readonly DynamicNinePatchBody ContentPanel;

    public Menu()
    {
        this.ContentPanel_OpenTransition = new()
        {
            Duration = 0.5d,
            EaseIn = MathC.EaseOutQuad,
            EaseOut = MathC.EaseOutQuad,
        };

        this.ContentPanel = new(new Rectangle(800, 300, 500, 300), "UI\\Borders\\Frame1.png", 10, 10, 990, 990, true);
        this.ContentPanel.Bounds.X += this.ContentPanel.Left; // Ensure it is FULLY off of screen (incase somebody overwrites frame1.png)

        var ButtonHover = GetShader("HoverButton");
        var ButtonUnavailable = GetShader("UnavailableButton");

        this.Slider = new(
            new(400, 200, 200, 10), "Slider.png"
        );


        this.Buttons = new(
            new ButtonWithText(
                new Rectangle(70, 310, 192, 32), "UI\\Buttons\\MenuButton.png", "Continue", "Clangen"
            )
            { 
                IsEnabled = false
            },
            new ButtonWithText(
                new Rectangle(70, 355, 192, 32), "UI\\Buttons\\MenuButton.png", "Play", "Clangen"
            )
            {
                OnClick = Button_ChooseClan
            },
            new ButtonWithText(
                new Rectangle(70, 400, 192, 32), "UI\\Buttons\\MenuButton.png", "Settings", "Clangen"
            ),
            new ButtonWithText(
                new Rectangle(70, 445, 192, 32), "UI\\Buttons\\MenuButton.png", "Content", "Clangen"
            )
            {
                OnClick = (_) =>
                {
                    this.ContentPanel.IsVisible = true;
                    
                    this.ContentPanel_OpenTransition.Start();
                    this.ContentPanel_OpenTransition.Reverse();
                }
            },
            new ButtonWithText(
                new Rectangle(70, 490, 192, 32), "UI\\Buttons\\MenuButton.png", "Quit", "Clangen"
            )
        )
        {
            Begin = (K) => K.Begin(samplerState: SamplerState.PointClamp),
            BeginHovered = (K) => K.Begin(effect: ButtonHover, samplerState: SamplerState.PointClamp),
            BeginUnavailable = (K) => K.Begin(effect: ButtonUnavailable, samplerState: SamplerState.PointClamp),
        };
    }

    void IScene.Update(GameTime GameTime)
    {
        this.Buttons.Update(GameTime);

        this.ContentPanel_OpenTransition.Update();
        this.ContentPanel.SetPosition((int)(300 + (500 + this.ContentPanel.Left) * (1 - this.ContentPanel_OpenTransition.T)), 300);

        if (this.ContentPanel_OpenTransition.T == 0.0d)
        {
            //this.ContentPanel.IsVisible = false;
        }
    }

    void IScene.Draw(SpriteBatchEx Batch)
    {
        Batch.Begin();
        DrawTexture("Background.png", new Rectangle(0, 0, 800, 700), Color.White);
        Batch.End();

        this.Buttons.Draw(Batch);



        Batch.Begin();

        if (this.ContentPanel.IsVisible)
            this.ContentPanel.Draw(Batch);
        

        DrawFont("Clangen", $"FPS: {ClangenNetGame.FPS.Average}", new (300, 100, 100, 20), Color.Red, null);
        Batch.End();
    }


    


    public void Button_ChooseClan(MouseState State)
    {
        SetScene<ChooseClan>();
    }
}




[SceneInfo("ChooseClan", true)]
public sealed class ChooseClan : IScene // TODO setup proper clan switching and loading functions
{
    private readonly World[] ExistingWorlds;

    public ChooseClan()
    {
        List<World> ExistingWorlds = [];
        foreach (string Path in Directory.GetFiles(SaveDataPath.FullName))
        {
            World? PossibleWorld = World.Load(Path);

            if (PossibleWorld is not null)
                ExistingWorlds.Add(PossibleWorld);
        }

        this.ExistingWorlds = [.. ExistingWorlds];
    }

    void IScene.Open()
    {
        World.Set(new World(1, GamemodeType.Classic, Season.Spring, "Save.xml"));
        SetScene<GroundScene>();
    }

    void IScene.Draw(SpriteBatchEx Batch)
    {

    }

    void IScene.Update(GameTime GameTime)
    {

    }
}