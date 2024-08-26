using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;

namespace ClangenReborn.Graphics;

public class Animation
{
    /// <summary>
    /// A value between 0 and 1 representing what percentage of the animation has played.
    /// </summary>
    public double Progress { get; private set; }
    private double T;

    public int ProgressPerSecond = 0;

    public Func<double, double>? EasingFunction = null;

    public void Update()
    { 
        if (this.EasingFunction is not null)
        {
            this.T = this.EasingFunction(this.Progress);
        }
        else
        {
            this.T = (this.Progress += (this.ProgressPerSecond * Content.Delta));
        }
    }

    public void Draw()
    {

    }
}



public class Progress
{
    private double InternalTimer = 0;
    private double InternalProgress = 0;
    public double Duration = 1;
    public double Step = 0.005d;


    public double T
    {
        get
        {
            if (!this.IsReversed && this.EaseIn is not null)
            {
                return this.EaseIn(this.InternalProgress);
            }
            else if (this.EaseOut is not null)
            {
                return this.EaseOut(this.InternalProgress);
            }
            else
            {
                return this.InternalProgress;
            }
        }
    }

    private double StepInterval => (this.Duration * this.Step);

    public Func<double, double>? EaseIn = null;
    public Func<double, double>? EaseOut = null;


    public Action? OnComplete = null;

    public bool IsInProgress = false;
    public bool IsReversed = false;
    public bool IsComplete => this.InternalProgress == 1.0d;


    public void Start()
    {
        this.IsInProgress = true;
    }

    public void Reset()
    {
        this.IsInProgress = false;
        this.InternalProgress = 0;
        this.InternalTimer = 0;
    }

    public void Restart()
    {
        Reset();
        Start();
    }

    public void Reverse()
    {
        this.IsReversed = !this.IsReversed;
    }

    public void Update()
    {
        if (!this.IsInProgress)
            return;

        if (!this.IsReversed)
        {
            if (this.InternalProgress >= 1)
                return;

            this.InternalTimer += Content.Delta;
            if (this.InternalTimer > this.StepInterval)
            {
                if ((this.InternalProgress += this.Step * (this.InternalTimer / this.StepInterval)) > 1.0d)
                {
                    this.InternalProgress = 1.0d;
                    this.OnComplete?.Invoke();
                }

                this.InternalTimer = 0;
            }
        }
        else
        {
            if (this.InternalProgress <= 0)
                return;

            this.InternalTimer += Content.Delta;
            if (this.InternalTimer > this.StepInterval)
            {
                if ((this.InternalProgress -= this.Step * (this.InternalTimer / this.StepInterval)) < 0.0d)
                {
                    this.InternalProgress = 0.0d;
                }

                this.InternalTimer = 0;
            }
        }
    }
}