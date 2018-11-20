﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//object that is rendered in a continuous manner
class Linear3DObject{
	public Material linearMaterial, crossMaterial;
    public float dashLength, dashInterval;
    public Polygon cross_section;
    public float offset;
    public string tag;
    public Linear3DObject(string name, float param = 0f){
        //TODO: read config dynamically
        //TODO: non-symmetry case
        tag = name;
        switch (name)
        {
            case "fence":
                {
                    crossMaterial = Resources.Load<Material>("Materials/roadBarrier1");
                    linearMaterial = Resources.Load<Material>("Materials/roadBarrier2");
                    Vector2 P0 = new Vector2(0.1f, 0f);
                    Vector2 P1 = new Vector2(0.1f, 1.0f);
                    Vector2 P2 = new Vector2(0f, 1.0f);
                    Vector2 P3 = new Vector2(-0.1f, 1.0f);
                    Vector2 P4 = new Vector2(-0.1f, 0f);
                    cross_section = new Polygon(new List<Curve> { new Line(P0, P1), new Arc(P2, P1, Mathf.PI), new Line(P3, P4), new Line(P4, P0) });
                    dashLength = 0.2f;
                    dashInterval = 2f;
                }
                break;
            case "lowbar":
                {
                    crossMaterial = linearMaterial = Resources.Load<Material>("Materials/white");
                    Vector2 P0 = new Vector2(0f, 0.4f);
                    Vector2 P1 = new Vector2(0.1f, 0.4f);
                    Vector2 P2 = new Vector2(-0.1f, 0.4f);
                    cross_section = new Polygon(new List<Curve> { new Arc(P0, P1, Mathf.PI), new Arc(P0, P2, Mathf.PI) });
                    dashInterval = 0f;
                }
                break;
            case "highbar":
                {
                    crossMaterial = linearMaterial = Resources.Load<Material>("Materials/white");
                    Vector2 P0 = new Vector2(0f, 0.9f);
                    Vector2 P1 = new Vector2(0.1f, 0.9f);
                    Vector2 P2 = new Vector2(-0.1f, 0.9f);
                    cross_section = new Polygon(new List<Curve> { new Arc(P0, P1, Mathf.PI), new Arc(P0, P2, Mathf.PI) });
                    dashInterval = 0f;
                }
                break;

            case "squarecolumn":
                linearMaterial = crossMaterial = Resources.Load<Material>("Materials/concrete");
                cross_section = new Polygon(new List<Curve>{
                    new Line(new Vector2(0.5f, -0.2f), new Vector2(-0.5f, -0.2f)),
                    new Line(new Vector2(-0.5f, -0.2f), new Vector2(-0.5f, -1f)),
                    new Line(new Vector2(-0.5f, -1f), new Vector2(0.5f, -1f)),
                    new Line(new Vector2(0.5f, -1f), new Vector2(0.5f, -0.2f))
                },
                                            new List<float>{
                    0f, 
                    0f,
                    -1.0f,
                    -1.0f
                }
                );

                dashLength = 1f;
                dashInterval = 8f;
                break;
            case "crossbeam":
                linearMaterial = crossMaterial = Resources.Load<Material>("Materials/concrete");
                if (param > 0f)
                {
                    setParam(param);
                }
                dashLength = 1f;
                dashInterval = 8f;
                break;
            case "bridgepanel":
                linearMaterial = Resources.Load<Material>("Materials/roadsurface");
                crossMaterial = Resources.Load<Material>("Materials/concrete");
                if (param > 0f){
                    setParam(param);
                }
                dashInterval = 0f;
                break;
            default:
                break;
        }
    }

    public void setParam(float param){
        switch(tag){
            case "crossbeam":
                cross_section = new Polygon(new List<Curve>{
                        new Line(new Vector2(param/2, -0.2f), new Vector2(-param/2, -0.2f)),
                        new Line(new Vector2(-param/2, -0.2f), new Vector2(-param/2, -1f)),
                        new Line(new Vector2(-param/2, -1f), new Vector2(-1f, -1.3f)),
                        new Line(new Vector2(-1f, -1.3f), new Vector2(1f, -1.3f)),
                        new Line(new Vector2(1f, -1.3f), new Vector2(param/2, -1f)),
                        new Line(new Vector2(param/2, -1f), new Vector2(param/2, -0.2f))
                    });
                break;
            case "bridgepanel":
                cross_section = new Polygon(new List<Curve>
                {
                    new Line(new Vector2(param/2, 0f), new Vector2(-param/2, 0f)),
                    new Line(new Vector2(-param/2, 0f), new Vector2(-param/2, -0.2f)),
                    new Line(new Vector2(-param/2, -0.2f), new Vector2(param/2, -0.2f)),
                    new Line(new Vector2(param/2, -0.2f), new Vector2(param/2, 0f))
                });
                break;
        }
    }
}

//objects rendered in discontinues manner
class NonLinear3DObject{
    public GameObject obj;
    public float interval;
}

class Separator
{
    public Texture texture;
    public bool dashed;
    public float offset;
}

/*should support:
lane 
interval
surface_{width}
removal_{width}

yellow/white/blueindi_dash/solid

should also support:
barrier
*/
public class RoadRenderer : MonoBehaviour
{

    public GameObject rend;
    public static float laneWidth = 2.8f;
    public static float separatorWidth = 0.2f;
    public static float separatorInterval = 0.2f;
    public static float fenceWidth = 0.2f;

    public float dashLength = 4f;
    public float dashInterval = 6f;

    public float dashIndicatorLength = 1f;
    public float dashIndicatorWidth = 2f;

    public void generate(Curve curve, List<string> laneConfig,
                         float indicatorMargin_0 = 0f, float indicatorMargin_1 = 0f, float surfaceMargin_0 = 0f, float surfaceMargin_1 = 0f,
                         bool indicator = false){

        Debug.Assert(surfaceMargin_0 <= indicatorMargin_0);
        Debug.Assert(indicatorMargin_0 < curve.length - indicatorMargin_1);
        Debug.Assert(surfaceMargin_1 <= indicatorMargin_1);
        if (Algebra.isclose(curve.z_offset, 0f) || (indicatorMargin_0 == 0f && indicatorMargin_1 == 0f)){
            //Debug.Log("generating single in the first place with 0= " + indicatorMargin_0 + " 1= " + indicatorMargin_1);
            generateSingle(curve, laneConfig, indicatorMargin_0, indicatorMargin_1, surfaceMargin_0, surfaceMargin_1, indicator);
            return;
        }
        else{
            //Debug.Log(curve + " ---" + indicatorMargin_0 + " : " + indicatorMargin_1);
            if (indicatorMargin_0 > 0){
                Curve margin0Curve = curve.cut(0f, indicatorMargin_0 / curve.length);
                margin0Curve.z_start = curve.at(0f).y;
                margin0Curve.z_offset = 0f;
                generateSingle(margin0Curve, laneConfig, indicatorMargin_0, 0f, surfaceMargin_0, 0f, indicator);
            }
            Curve middleCurve = curve.cut(indicatorMargin_0 / curve.length, 1f - indicatorMargin_1 / curve.length);
            middleCurve.z_start = curve.at(0f).y;
            middleCurve.z_offset = curve.at(1f).y - curve.at(0f).y;
            generateSingle(middleCurve, laneConfig, 0f, 0f, 0f, 0f, indicator);
            //Debug.Log("generating single in the 2nd place");

            if (indicatorMargin_1 > 0){
                Curve margin1Curve = curve.cut(1f - indicatorMargin_1/curve.length, 1f);
                margin1Curve.z_start = curve.at(1f).y;
                margin1Curve.z_offset = 0f;
                generateSingle(margin1Curve, laneConfig, 0f, indicatorMargin_1, 0f, surfaceMargin_1, indicator);
            }
        }
    }

    void generateSingle(Curve curve, List<string> laneConfig, 
                         float indicatorMargin_0 , float indicatorMargin_1 , float surfaceMargin_0 , float surfaceMargin_1,
                         bool indicator)
    {
        List<Separator> separators = new List<Separator>();
        List<Linear3DObject> linear3DObjects = new List<Linear3DObject>();
        float offset = 0f;

        foreach (string l in laneConfig)
        {
            string[] configs = l.Split('_');
            List<string> commonTypes = new List<string> { "lane", "interval", "surface", "removal", "fence", "column" };
            if (commonTypes.Contains(configs[0]))
            {
                switch (configs[0])
                {
                    case "lane":
                        offset += laneWidth;
                        break;
                    case "interval":
                        offset += separatorInterval;
                        break;
                    case "surface":
                        Linear3DObject roadBlock = new Linear3DObject("bridgepanel");
                        linear3DObjects.Add(roadBlock);
                        if (configs.Length > 1)
                        {
                            offset += float.Parse(configs[1]);
                        }
                        break;
                    case "column":
                        Linear3DObject squarecolumn = new Linear3DObject("squarecolumn");
                        Linear3DObject crossbeam = new Linear3DObject("crossbeam");
                        linear3DObjects.Add(squarecolumn);
                        linear3DObjects.Add(crossbeam);
                        break;
                    case "fence":
                        Linear3DObject fence = new Linear3DObject("fence");
                        Linear3DObject lowbar = new Linear3DObject("lowbar");
                        Linear3DObject highbar = new Linear3DObject("highbar");

                        fence.offset = lowbar.offset = highbar.offset = offset + fenceWidth / 2;
                        linear3DObjects.Add(fence);
                        linear3DObjects.Add(lowbar);
                        linear3DObjects.Add(highbar);
                        offset += fenceWidth;
                        break;
                    case "removal":
                        offset += float.Parse(configs[1]);
                        drawRemovalMark(curve, offset);
                        return;
                }
            }
            else
            {
                string septype, sepcolor;
                septype = configs[0];
                sepcolor = configs[1];
                
                Separator sep = new Separator();

                switch (sepcolor)
                {
                    case "yellow":
                        sep.texture = Resources.Load<Texture>("Textures/yellow");
                        break;
                    case "white":
                        sep.texture = Resources.Load<Texture>("Textures/white");
                        break;
                    case "blueindi":
                        sep.texture = Resources.Load<Texture>("Textures/blue");
                        break;
                }

                switch (septype)
                {
                    case "dash":
                        sep.dashed = true;
                        break;
                    case "solid":
                        sep.dashed = false;
                        break;
                }

                sep.offset = offset;

                separators.Add(sep);

                offset += separatorWidth;
            }

        }

        //adjust center
        if (!Algebra.isclose(indicatorMargin_0 + indicatorMargin_1, curve.length))
        {
            for (int i = 0; i != separators.Count; i++)
            {
                separators[i].offset -= offset / 2;
                drawLinear2DObject(curve, separators[i], indicatorMargin_0, indicatorMargin_1);
            }
        }

        for (int i = 0; i != linear3DObjects.Count; i++)
        {
            {
                Linear3DObject obj = linear3DObjects[i];
                switch(obj.tag){
                    case "crossbeam":
                        if ((curve.z_start > 0 || curve.z_offset > 0) && !Algebra.isclose(indicatorMargin_0 + indicatorMargin_1, curve.length)) {
                            linear3DObjects[i].setParam(offset);
                            drawLinear3DObject(curve, linear3DObjects[i], indicatorMargin_0, indicatorMargin_1);
                        }
                        break;
                    case "squarecolumn":
                        if ((curve.z_start > 0 || curve.z_offset > 0) && !Algebra.isclose(indicatorMargin_0 + indicatorMargin_1, curve.length)){
                            drawLinear3DObject(curve, linear3DObjects[i], indicatorMargin_0, indicatorMargin_1);
                        }
                        break;
                    case "bridgepanel":
                        if (!Algebra.isclose(surfaceMargin_0 + surfaceMargin_1, curve.length)){
                            linear3DObjects[i].offset = 0f;
                            linear3DObjects[i].setParam(offset);
                            drawLinear3DObject(curve, linear3DObjects[i], surfaceMargin_0, surfaceMargin_1);
                        }
                        break;
                    default:
                        linear3DObjects[i].offset -= offset / 2;

                        if ((curve.z_start > 0 || curve.z_offset > 0) && !Algebra.isclose(indicatorMargin_0 + indicatorMargin_1, curve.length))
                        {
                            drawLinear3DObject(curve, linear3DObjects[i], indicatorMargin_0, indicatorMargin_1);
                        }
                        break;
                }
            }
        }


    }

	void drawLinear2DObject(Curve curve, Separator sep, float margin_0 = 0f, float margin_1 = 0f){
        if (curve.length > 0 && (margin_0 > 0 || margin_1 > 0))
        {
            curve = curve.cut(margin_0 / curve.length, 1f - margin_1 / curve.length);
        }
		if (!sep.dashed) {
			GameObject rendins = Instantiate (rend, transform);
            rendins.transform.parent = this.transform;
			CurveRenderer decomp = rendins.GetComponent<CurveRenderer> ();
            Material normalMaterial = new Material(Shader.Find("Standard"));
            normalMaterial.mainTexture = sep.texture;
            decomp.CreateMesh (curve, separatorWidth, normalMaterial, offset: sep.offset + separatorWidth / 2, z_offset:0.01f);
		}
		else {
            List<Curve> dashed = curve.segmentation (dashLength + dashInterval);
			foreach (Curve singledash in dashed) {
                List<Curve> vacant_and_dashed = singledash.split(dashInterval / (dashLength + dashInterval), byLength:true);

                if (vacant_and_dashed.Count == 2) {
					GameObject rendins = Instantiate (rend, transform);
					CurveRenderer decomp = rendins.GetComponent<CurveRenderer> ();
                    Material normalMaterial = new Material(Shader.Find("Standard"));
                    normalMaterial.mainTexture = sep.texture;
                    decomp.CreateMesh (vacant_and_dashed [1], separatorWidth, normalMaterial, offset:sep.offset + separatorWidth / 2, z_offset:0.01f);
				}

			}

		}
	}

    void drawLinear3DObject(Curve curve, Linear3DObject obj, float margin_0 = 0f, float margin_1 = 0f){
        Debug.Assert(margin_0 >= 0 && margin_1 >= 0);
        if (curve.length > 0 && (margin_0 > 0 || margin_1 > 0)){
            curve = curve.cut(margin_0 / curve.length, 1f - margin_1 / curve.length);
        }
        if (obj.dashInterval == 0f)
        {
            GameObject rendins = Instantiate(rend, transform);
            rendins.transform.parent = this.transform;
            CurveRenderer decomp = rendins.GetComponent<CurveRenderer>();
            decomp.CreateMesh(curve, obj.offset, obj.linearMaterial, obj.crossMaterial, obj.cross_section);
        }
        else
        {
            Debug.Assert(obj.dashLength > 0f);
            List<Curve> dashed = curve.segmentation(obj.dashLength + obj.dashInterval);
            foreach (Curve singledash in dashed)
            {
                List<Curve> vacant_and_dashed = singledash.split(obj.dashInterval / (obj.dashLength + obj.dashInterval), byLength: true);
                if (vacant_and_dashed.Count == 2)
                {
                    GameObject rendins = Instantiate(rend, transform);
                    rendins.transform.parent = this.transform;
                    CurveRenderer decomp = rendins.GetComponent<CurveRenderer>();
                    decomp.CreateMesh(vacant_and_dashed[1], obj.offset, obj.linearMaterial, obj.crossMaterial, obj.cross_section);
                }
            }
        }
    }

    void drawRemovalMark(Curve curve, float width){
        GameObject rendins = Instantiate(rend, transform);
        rendins.transform.parent = this.transform;
        CurveRenderer decomp = rendins.GetComponent<CurveRenderer>();
        Material normalMaterial = new Material(Shader.Find("Standard"));
        normalMaterial.mainTexture = Resources.Load<Texture>("Textures/orange");
        decomp.CreateMesh(curve, width, normalMaterial, z_offset:0.02f);
    }

    public static float getConfigureWidth(List<string> laneconfigure){
        var ans = 0f;
        for (int i = 0; i != laneconfigure.Count; ++i)
        {
            switch (laneconfigure[i])
            {
                case "lane":
                    ans += laneWidth;
                    break;
                case "interval":
                    ans += separatorInterval;
                    break;
                case "separator":
                    ans += separatorWidth;
                    break;
                case "fence":
                    ans += fenceWidth;
                    break;
            }
        }
        return ans;
    }

}
