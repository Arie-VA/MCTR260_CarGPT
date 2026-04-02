using UnityEngine;
using System.Collections.Generic;

public class PathFollower
{
    private RobotConfig ctrlCfg;

    public PathFollower(RobotConfig control)
    {
        ctrlCfg = control;
    }

    public void Initialize() { } // kept for compatibility with WarehouseAIController

    public struct PathPoint
    {
        public Vector3 position;
        public float   targetHeading; // radians, Atan2(z, x) world space
        public bool    isCorner;
    }

    public List<PathPoint> SetPath(Vector3[] corners)
    {
        var waypoints = BuildWaypoints(corners, ctrlCfg.waypointSpacing);
        waypoints = StampHeadings(waypoints);
        return waypoints;
    }

    // ── Dense interpolation ───────────────────────────────────────────────
    private List<PathPoint> BuildWaypoints(Vector3[] corners, float spacing)
    {
        var pts = new List<PathPoint>();
        if (corners.Length == 0) return pts;

        for (int i = 0; i < corners.Length - 1; i++)
        {
            Vector3 a      = corners[i];
            Vector3 b      = corners[i + 1];
            float   segLen = Vector3.Distance(a, b);
            int     count  = Mathf.Max(1, Mathf.RoundToInt(segLen / spacing));

            pts.Add(new PathPoint { position = a, isCorner = true });

            for (int j = 1; j < count; j++)
            {
                float t = (float)j / count;
                pts.Add(new PathPoint { position = Vector3.Lerp(a, b, t), isCorner = false });
            }
        }

        pts.Add(new PathPoint { position = corners[corners.Length - 1], isCorner = true });
        return pts;
    }

    // ── Heading = direction to next waypoint ──────────────────────────────
    private List<PathPoint> StampHeadings(List<PathPoint> pts)
    {
        for (int i = 0; i < pts.Count - 1; i++)
        {
            Vector3 dir     = pts[i + 1].position - pts[i].position;
            float   heading = Mathf.Atan2(dir.z, dir.x);
            pts[i] = new PathPoint {
                position      = pts[i].position,
                targetHeading = heading,
                isCorner      = pts[i].isCorner
            };
        }

        // Last point inherits previous heading
        if (pts.Count > 1)
        {
            var last = pts[pts.Count - 1];
            pts[pts.Count - 1] = new PathPoint {
                position      = last.position,
                targetHeading = pts[pts.Count - 2].targetHeading,
                isCorner      = last.isCorner
            };
        }

        return pts;
    }
}