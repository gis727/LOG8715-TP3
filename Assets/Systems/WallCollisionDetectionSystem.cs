using UnityEngine;

public class WallCollisionDetectionSystem : ISystem
{
    public string Name
    {
        get
        {
            return GetType().Name;
        }
    }

    private enum ScreenBorderSide
    {
        None,
        Left,
        Right,
        Top,
        Bottom
    }
    public void UpdateSystem()
    {
        ComponentsManager.Instance.ForEach<ShapeComponent>((entity, shapeComponent) =>
        {
            Vector2 speed = shapeComponent.speed;
            Vector2 screenBorderPos = Camera.main.ScreenToWorldPoint(new Vector2(Screen.width, Screen.height));

            // detect wall collision
            var pos = shapeComponent.pos;
            float radius = shapeComponent.size / 2;
            bool wallCollision = false;
            ScreenBorderSide currentWallCollisionType = ScreenBorderSide.None;
            if (pos.x + radius >= screenBorderPos.x || pos.x - radius <= -screenBorderPos.x)
            {
                wallCollision = true;
                if (pos.x + radius >= screenBorderPos.x)
                {
                    currentWallCollisionType = ScreenBorderSide.Right;
                }
                else
                {
                    currentWallCollisionType = ScreenBorderSide.Left;
                }
            }
            if (pos.y + radius >= screenBorderPos.y || pos.y - radius <= -screenBorderPos.y)
            {
                wallCollision = true;
                if (pos.y + radius >= screenBorderPos.y)
                {
                    currentWallCollisionType = ScreenBorderSide.Top;
                }
                else
                {
                    currentWallCollisionType = ScreenBorderSide.Bottom;
                }
            }

            // apply change of speed, position and size
            if (wallCollision)
            {
                switch (currentWallCollisionType)
                {
                    case ScreenBorderSide.Right:
                        speed.x = -speed.x;
                        pos.x = screenBorderPos.x - radius;
                        break;
                    case ScreenBorderSide.Left:
                        speed.x = -speed.x;
                        pos.x = -screenBorderPos.x + radius;
                        break;
                    case ScreenBorderSide.Top:
                        speed.y = -speed.y;
                        pos.y = screenBorderPos.y - radius;
                        break;
                    case ScreenBorderSide.Bottom:
                        speed.y = -speed.y;
                        pos.y = -screenBorderPos.y + radius;
                        break;
                }
                shapeComponent.pos = pos;
                shapeComponent.speed = speed;
                ComponentsManager.Instance.SetComponent<ShapeComponent>(entity, shapeComponent);
            }
        });
    }
}
