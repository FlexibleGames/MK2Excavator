using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Valve.VR;

public class DigArea
{
    private CubeCoord origin;
    private int curHeight;

    private Vector3 vectorUp;
    private Vector3 vectorForward;
    private Vector3 vectorRight;

    private int digRadius;
    private int maxHeight;

    public DigArea(CubeCoord origin, int curHeight, int digRadius, int maxHeight, byte flags)
    {
        // Need to know where the block is
        this.origin = origin;

        // Current dig position (offset from block)
        this.curHeight = curHeight;

        // Dig settings
        this.digRadius = digRadius;
        this.maxHeight = maxHeight;

        // We'll need these to math later
        var rotationQuart = SegmentCustomRenderer.GetRotationQuaternion(flags);

        vectorUp = rotationQuart * Vector3.up;
        vectorForward = rotationQuart * Vector3.forward;
        vectorRight = rotationQuart * Vector3.right;

        vectorUp.Normalize();
        vectorForward.Normalize();
        vectorRight.Normalize();
    }

    public IEnumerator<CubeCoord> GetRemainingDigArea()
    {
        var height = 1;
        if (curHeight > 0 && curHeight < maxHeight)
        {
            height = curHeight;
        }

        for (; height <= maxHeight; height++)
        {
            for (var right = -digRadius; right <= digRadius; right++)
            {
                for (var forward = -digRadius; forward <= digRadius; forward++)
                {
                    var x = origin.x + (long) ((double) height * vectorUp.x);
                    var y = origin.y + (long) ((double) height * vectorUp.y);
                    var z = origin.z + (long) ((double) height * vectorUp.z);

                    x += (long) ((double) right * vectorRight.x);
                    y += (long) ((double) right * vectorRight.y);
                    z += (long) ((double) right * vectorRight.z);

                    x += (long) ((double) forward * vectorForward.x);
                    y += (long) ((double) forward * vectorForward.y);
                    z += (long) ((double) forward * vectorForward.z);

                    curHeight = height;

                    yield return new CubeCoord(x, y, z);
                }
            }
        }
    }

    public int Volume => (this.digRadius * 4 + 2) * this.maxHeight;
    public int CurrentHeight => curHeight;
}