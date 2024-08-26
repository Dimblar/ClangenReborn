using ClangenReborn.Graphics;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

namespace ClangenReborn;



public static partial class Content
{
    private static readonly Dictionary<Type, IScene> SceneInstances = [];
    private static readonly HashSet<Type> SceneTypes = [];

    /// <summary>
    /// The Current Scene.
    /// </summary>
    public static IScene? CurrentScene { get; private set; }

    public static void SetScene<TScene>() where TScene : class, IScene
    {
        if (CurrentScene?.GetType() == typeof(TScene))
            return;

        SceneTypes.Add(typeof(TScene));

        if (SceneInstances.TryGetValue(typeof(TScene), out IScene? Existing))
        {
            CurrentScene = Existing;
        }
        else
        {
            TScene? New;

            if ((New = Activator.CreateInstance(typeof(TScene)) as TScene) is not null)
            {
                CurrentScene?.Close();
                SceneInstances[typeof(TScene)] = CurrentScene = New;
            }
            else
            {
                Log.Warning($"Failed to create Scene of type \"{typeof(TScene).FullName}\"");
                return;
            }
        }

        CurrentScene.Open();
    }
}

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class SceneInfoAttribute : Attribute
{
    public readonly string? Id;
    public readonly bool Discoverable;

    public SceneInfoAttribute() { }
    public SceneInfoAttribute(string? Id = null, bool Discoverable = true)
    {
        this.Id = Id;
        this.Discoverable = Discoverable;
    }
}

public interface IScene
{
    /// <summary>
    /// Method to be called every game tick.
    /// </summary>
    internal void Update(GameTime GameTime);

    /// <summary>
    /// Method to be called when drawing the scene.
    /// </summary>
    internal void Draw(SpriteBatchEx Batch);

    /// <summary>
    /// Method to be called when drawing the scene, only if debug mode is enabled. Defaults to <see cref="IScene.Draw(SpriteBatchEx)"/>.
    /// </summary>
    internal void DrawDebug(SpriteBatchEx Batch) => Draw(Batch);

    /// <summary>
    /// Method to be called when switching to another scene.
    /// </summary>
    internal void Close() { }

    /// <summary>
    /// Method to be called when switching to this scene.
    /// </summary>
    internal void Open() { }
}