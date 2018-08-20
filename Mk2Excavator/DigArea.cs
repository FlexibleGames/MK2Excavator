using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
//using Valve.VR;

public class DigArea
{
    private CubeCoord Origin;
    private int CurHeight;

    private Vector3 vectorUp;
    private Vector3 vectorForward;
    private Vector3 vectorRight;

    private int DigRadius;
    private int MaxHeight;

    public int Volume; 
    public int CurrentHeight;

    public DigArea(CubeCoord origin, int curHeight, int digRadius, int maxHeight, byte flags)
    {
        // Need to know where the block is
        this.Origin = origin;

        // Current dig position (offset from block)
        this.CurHeight = curHeight;

        // Dig settings
        this.DigRadius = digRadius;
        this.MaxHeight = maxHeight;

        Volume = ((digRadius * 2) + 1) * maxHeight;
        CurrentHeight = curHeight;

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
        if (CurHeight > 0 && CurHeight < MaxHeight)
        {
            height = CurHeight;
        }

        for (; height <= MaxHeight; height++)
        {
            for (var right = -DigRadius; right <= DigRadius; right++)
            {
                for (var forward = -DigRadius; forward <= DigRadius; forward++)
                {
                    var x = Origin.x + (long) ((double) height * vectorUp.x);
                    var y = Origin.y + (long) ((double) height * vectorUp.y);
                    var z = Origin.z + (long) ((double) height * vectorUp.z);

                    x += (long) ((double) right * vectorRight.x);
                    y += (long) ((double) right * vectorRight.y);
                    z += (long) ((double) right * vectorRight.z);

                    x += (long) ((double) forward * vectorForward.x);
                    y += (long) ((double) forward * vectorForward.y);
                    z += (long) ((double) forward * vectorForward.z);

                    CurHeight = height;
                    CurrentHeight = height;

                    yield return new CubeCoord(x, y, z);
                }
            }
        }
    }
}