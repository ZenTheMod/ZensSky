using Microsoft.Xna.Framework;
using System;

namespace ZensSky.Core.Utils;

public static class Easings
{
    public static float InPolynomial(float t, float e) =>
        MathF.Pow(t, e);
    public static float OutPolynomial(float t, float e) =>
        1 - InPolynomial(1 - t, e);
    public static float InOutPolynomial(float t, float e) =>
        t < .5 ?
            (1 - InPolynomial((1 - t) * 2, e) * .5f) :
            (InPolynomial(t * 2, e) * .5f);

    public static float InSine(float t) =>
        1 - MathF.Cos(t * MathHelper.PiOver2);
    public static float OutSine(float t) =>
        MathF.Sin(t * MathHelper.PiOver2);
    public static float InOutSine(float t) =>
        (MathF.Cos(t * MathF.PI) - 1) * -.5f;

    public static float InExpo(float t) =>
        MathF.Pow(2, 10 * (t - 1));
    public static float OutExpo(float t) =>
        1 - InExpo(1 - t);
    public static float InOutExpo(float t) =>
        t < .5 ? 
            InExpo(t * 2) * .5f :
            1 - InExpo((1 - t) * 2) * .5f;

    public static float InCirc(float t) =>
        -(MathF.Sqrt(1 - t * t) - 1);
    public static float OutCirc(float t) =>
        1 - InCirc(1 - t);
    public static float InOutCirc(float t) =>
        t < .5 ?
            InCirc(t * 2) * .5f :
            1 - InCirc((1 - t) * 2) * .5f;

    public static float InElastic(float t, float p = .3f) =>
        1 - OutElastic(1 - t, p);
    public static float OutElastic(float t, float p = .3f) =>
        MathF.Pow(2, -10 * t) * MathF.Sin((t - p / 4) * (2 * MathF.PI) / p) + 1;
    public static float InOutElastic(float t, float p = .3f) =>
        t < .5 ?
            InElastic(t * 2, p) * .5f :
            1 - InElastic((1 - t) * 2, p) * .5f;

    public static float InBack(float t, float s = 1.7f) =>
        t * t * ((s + 1) * t - s);
    public static float OutBack(float t, float s = 1.7f) =>
        1 - InBack(1 - t, s);
    public static float InOutBack(float t, float s = 1.7f) =>
        t < .5 ?
            InBack(t * 2, s) * .5f :
            1 - InBack((1 - t) * 2, s) * .5f;
}
