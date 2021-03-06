using Godot;
using System.Collections.Generic;

public class Card : Node2D
{
    [Signal] public delegate void IconSelected(Card card, string name);

    public const int CARD_SIZE = 400;

    private string[] icons;
    private RandomNumberGenerator rng;

    public int cardIndex;


    public override void _Ready()
    {
    }

    public void InitCard(int cardIndex)
    {
        this.cardIndex = cardIndex;
        this.icons = Fobble.BASE_DECK[cardIndex];;

        rng = new RandomNumberGenerator();
        rng.Randomize();

        PackedScene iconScene = GD.Load<PackedScene>("res://Icon.tscn");

        List<Vector2> places = null;
        bool gotPlaces = false;

        //Brute force card generation. Failure is not an option.
        while (!gotPlaces)
        {
            try
            {
                //64 pixels * sqrt(2) ~= 91 pixels
                places = GeneratePoisson(CARD_SIZE, CARD_SIZE, 91f, icons.Length);
                gotPlaces = true;
            }
            catch {}
        }

        for (int i = 0; i < icons.Length; i++)
        {
            Icon icon = (Icon)iconScene.Instance();

            Texture tex = GD.Load<Texture>("res://" + icons[i] + ".png");
            icon.SetTexture(tex);
            icon.RotationDegrees = rng.RandfRange(0, 360);
            icon.Position = places[i];
            icon.Name = icons[i];

            icon.Connect("OnClick", this, "_on_Icon_OnClick");

            AddChild(icon);
            icon.Owner = this;
        }
    }

    public bool HasIcon(string icon)
    {
        for (int i = 0; i < icons.Length; i++)
        {
            if (icons[i] == icon)
                return true;
        }
        return false;
    }

    public static bool HasIcon(int cardIndex, string icon)
    {
        string[] icons = Fobble.BASE_DECK[Fobble.Instance.deckOrder[cardIndex]];
        for (int i = 0; i < icons.Length; i++)
        {
            if (icons[i] == icon)
                return true;
        }
        return false;
    }

    //Function to create Poisson disk sampled points so no icons overlap
    private List<Vector2> GeneratePoisson(int width, int height, float minDist, int numPoints)
    {
        List<Vector2> points = new List<Vector2>();
        List<Vector2> active = new List<Vector2>();

        float cellSize = minDist / Mathf.Sqrt2;

        int gridWidth = (int)((width) / cellSize);
        int gridHeight = (int)((height) / cellSize);

        Vector2[,] grid = new Vector2[gridWidth, gridHeight];

        //Create the first point
        Vector2 p0 = new Vector2(rng.RandfRange(minDist, width - minDist), rng.RandfRange(minDist, height - minDist));
        
        int xInd = Mathf.Min(gridWidth-1, Mathf.Max(0, Mathf.FloorToInt(p0.x / cellSize)));
        int yInd = Mathf.Min(gridHeight-1, Mathf.Max(0, Mathf.FloorToInt(p0.y / cellSize)));

        grid[xInd,yInd] = p0;

        points.Add(p0);
        active.Add(p0);

        while (active.Count > 0)
        {
            //Pick a random point
            int randInd = rng.RandiRange(0, active.Count-1);
            Vector2 currPoint = active[randInd];

            bool found = false;
            //Try a number of times to find a new point
            for (int tries = 0; tries < 200; tries++)
            {
                float theta = rng.RandfRange(0, Mathf.Tau);
                float radius = rng.RandfRange(minDist, minDist * 2);

                Vector2 newPoint = new Vector2(
                    currPoint.x + radius * Mathf.Cos(theta),
                    currPoint.y + radius * Mathf.Sin(theta)
                );

                //Check the point is in bounds of the card
                if (newPoint.x < minDist || newPoint.y < minDist || newPoint.x >= width - minDist || newPoint.y >= height - minDist)
                    continue;//Outside of the grid bounds, discard

                //Check it's a valid point
                xInd = Mathf.FloorToInt(newPoint.x / cellSize);
                yInd = Mathf.FloorToInt(newPoint.y / cellSize);

                int x0 = Mathf.Max(xInd - 1, 0);
                int x1 = Mathf.Min(xInd + 1, gridWidth-1);
                int y0 = Mathf.Max(yInd - 1, 0);
                int y1 = Mathf.Min(yInd + 1, gridHeight-1);

                bool valid = true;
                //Check neighbouring cells for existing points
                for (int x = x0; x <= x1; x++)
                {
                    for (int y = y0; y <= y1; y++)
                    {
                        if (grid[x,y] != null)
                            if (grid[x,y].DistanceTo(newPoint) < radius)
                                valid = false;//Too close to another point, discard

                        if (!valid)
                            break;
                    }
                    if (!valid)
                        break;
                }

                if (valid)
                {
                    grid[xInd,yInd] = newPoint;
                    points.Add(newPoint);
                    active.Add(newPoint);
                    found = true;
                    break;
                }
            }

            if (!found)
                active.RemoveAt(randInd);

            if (points.Count >= numPoints)
                break;
        }

        if (points.Count != numPoints)
            throw new System.Exception("Could not find a spot for all icons");

        return points;
    }

    private void _on_Icon_OnClick(string icon)
    {
        EmitSignal("IconSelected", this, icon);
    }
}
