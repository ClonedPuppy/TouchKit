using StereoKit;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace TouchMenuApp
{
    class UIElements
    {
        [StructLayout(LayoutKind.Sequential)]
        struct ButtonData
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
            public Vec4[] button;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct HsliderData
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
            public Vec4[] hSlider;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct VsliderData
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
            public Vec4[] vSlider;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct SliderValuesData
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
            public Vec4[] sliderValue;
        }
        struct Abilities
        {
            public int type;
            public string name;
            public int defState;
            public int minRange;
            public int maxRange;
        }

        Pose panelPose = new Pose(0, 0, -0.4f, Quat.FromAngles(-90, 180, 0));
        Model panel;
        Material panelMaterial;

        ButtonData buttons = new ButtonData();
        HsliderData hSliders = new HsliderData();
        VsliderData vSliders = new VsliderData();
        SliderValuesData sliderValues = new SliderValuesData();

        Bounds buttonBounds;
        Pose node;
        Pose PoseNeutral;
        Vec3 size;
        Mesh button;

        double interval;
        double interValTime;

        // Holds all the button abilities for the UI parsed from the blender file
        Dictionary<int, Abilities> buttonAbilities;

        // Constructor
        public UIElements()
        {
            buttonAbilities = new Dictionary<int, Abilities>();

            panelMaterial = new Material(Shader.FromFile("TouchPanelShader.hlsl"));
            panel = Model.FromFile("Panel_v001.glb");
            panelMaterial[MatParamName.DiffuseTex] = panel.Visuals[0].Material.GetTexture("diffuse");
            panelMaterial[MatParamName.MetalTex] = panel.Visuals[0].Material.GetTexture("metal");
            panel.Visuals[0].Material = panelMaterial;
            panelMaterial.Transparency = Transparency.Blend;

            size = new Vec3(0.02f, 0.01f, 0.02f);
            PoseNeutral = new Pose(V.XYZ(0, -0.01f, 0), Quat.FromAngles(90, 0, 0));
            buttonBounds = new Bounds(size);
            button = Mesh.GenerateCube(size);

            interval = 0.1d;
            interValTime = Time.Total + interval;

            // Is the panel landscape or portrait?
            float longestSide = FindLongestSide(panel);

            buttons.button = new Vec4[20];
            hSliders.hSlider = new Vec4[10];
            vSliders.vSlider = new Vec4[10];

            sliderValues.sliderValue = new Vec4[20];

            // Parse out buttons and sliders in the gltf file
            var i = 0;
            var j = 0;
            var k = 0;

            foreach (var item in panel.Nodes)
            {
                float _positionX = (item.LocalTransform.Pose.position.x + (panel.Bounds.dimensions.x / 2)) / longestSide;
                float _positionY = (item.LocalTransform.Pose.position.z + (panel.Bounds.dimensions.z / 2)) / longestSide;

                float metallic = 1;
                float roughness = 0.2f;

                if (item.Name != "panel")
                {
                    if (item.Info.Get("type") == "button")
                    {
                        buttons.button[i] = new Vec4(_positionX, _positionY, metallic, roughness);
                        i++;
                    }
                    else if (item.Info.Get("type") == "hslider")
                    {
                        hSliders.hSlider[j] = new Vec4(_positionX, _positionY, metallic, roughness);
                        j++;
                    }
                    else if (item.Info.Get("type") == "vslider")
                    {
                        vSliders.vSlider[k] = new Vec4(_positionX, _positionY, metallic, roughness);
                        k++;
                    }
                }

                // Send UI element setup data to the shader
                panelMaterial.SetInt("buttonAmount", i);
                panelMaterial.SetInt("hSliderAmount", j);
                panelMaterial.SetInt("vSliderAmount", k);
                panelMaterial.SetData<ButtonData>("button", buttons);
                panelMaterial.SetData<HsliderData>("hslider", hSliders);
                panelMaterial.SetData<VsliderData>("vslider", vSliders);
                panelMaterial.SetData<SliderValuesData>("sliderValue", sliderValues);
            }
        }

        public void DrawUI()
        {
            UI.Handle("Panel", ref panelPose, panel.Bounds);
            panel.Draw(panelPose.ToMatrix());

            Hierarchy.Push(panelPose.ToMatrix());
            var i = 0;
            var j = 9;
            foreach (var item in panel.Nodes)
            {
                if (item.Name != "panel")
                {
                    if (item.Info.Get("type") == "button")
                    {
                        Button(panel, item.Name);
                    }
                    else if (item.Info.Get("type") == "hslider")
                    {
                        var value = sliderValues.sliderValue[i].x;
                        sliderValues.sliderValue[i].x = HSlider(panel, item.Name, value);
                        i++;
                    }
                    else if (item.Info.Get("type") == "vslider")
                    {
                        var value = sliderValues.sliderValue[j].x;
                        sliderValues.sliderValue[j].x = VSlider(panel, item.Name, value);
                        j++;
                    }
                }
            }
            Hierarchy.Pop();

            if (Time.Total > interValTime)
            {
                // Update the shader with new data derived from button and slider manipulations
                panelMaterial.SetData<SliderValuesData>("sliderValue", sliderValues);

                interValTime = Time.Total + interval;
            }
        }

        void Button(Model _model, string _nodeName)
        {
            node = _model.FindNode(_nodeName).ModelTransform.Pose;

            UI.PushSurface(node);
            UI.WindowBegin(_nodeName + "Win", ref PoseNeutral, UIWin.Empty);
            UI.ButtonBehavior(
                button.Bounds.dimensions.XZ.XY0 / 2,
                button.Bounds.dimensions.XZ,
                _nodeName,
                out float finger,
                out BtnState state,
                out BtnState focus);
            UI.WindowEnd();
            UI.PopSurface();

            if ((state & BtnState.JustActive) > 0)
            {
                //buttonStates[_nodeName] = buttonStates[_nodeName] == true ? false : true;
                //interValTime = Time.Total + interval;
                //if (buttonStates[_nodeName] == true)
                //{
                //    //System.Console.WriteLine(_nodeName.ToString() + " Pressed");
                //    //Assets.surfaceTopMat.SetFloat(_nodeName, 1);
                //    //if (nodeName == "Play")
                //    //{
                //    //    buttonStates["Stop"] = false;
                //    //    Assets.surfaceTopMat.SetFloat("Stop", 0);
                //    //}
                //    //else
                //    //{
                //    //    buttonStates["Play"] = false;
                //    //    Assets.surfaceTopMat.SetFloat("Play", 0);
                //    //}
                //}
                //else
                //{
                //    //Assets.surfaceTopMat.SetFloat(_nodeName, 0);
                //}
            }

            //if (!_sticky & buttonStates[_nodeName] == true & Time.Total > interValTime)
            //{
            //    buttonStates[_nodeName] = false;
            //    //Assets.surfaceTopMat.SetFloat(_nodeName, 0);
            //    interValTime += interval;
            //}
        }

        float HSlider(Model _model, string _nodeName, float currentValue)
        {
            node = _model.FindNode(_nodeName).ModelTransform.Pose;

            UI.PushSurface(node);
            Vec3 volumeAt = new Vec3(0, 0, 0);
            Vec3 volumeSize = new Vec3(0.065f, 0.01f, 0.01f);

            BtnState volumeState = UI.VolumeAt(_nodeName + "HVolume", new Bounds(volumeAt, volumeSize), UIConfirm.Push, out Handed hand);
            if (volumeState != BtnState.Inactive)
            {
                var result = System.Math.Clamp(Remap(Hierarchy.ToLocal(Input.Hand(hand)[FingerId.Index, JointId.Tip].Pose).position.x, -0.03f, 0.028f, 0.1f, 0.001f), 0, 0.2f);
                UI.PopSurface();
                return result;
            }
            UI.PopSurface();

            return currentValue;
        }

        float VSlider(Model _model, string _nodeName, float currentValue)
        {
            node = _model.FindNode(_nodeName).ModelTransform.Pose;

            UI.PushSurface(node);
            Vec3 volumeAt = new Vec3(0, 0, 0);
            Vec3 volumeSize = new Vec3(0.01f, 0.01f, 0.065f);

            BtnState volumeState = UI.VolumeAt(_nodeName + "VVolume", new Bounds(volumeAt, volumeSize), UIConfirm.Push, out Handed hand);
            if (volumeState != BtnState.Inactive)
            {
                var result = System.Math.Clamp(Remap(Hierarchy.ToLocal(Input.Hand(hand)[FingerId.Index, JointId.Tip].Pose).position.z, 0.03f, -0.028f, 0.11f, 0.001f), 0.001f, 0.11f);
                UI.PopSurface();
                return result;
            }
            UI.PopSurface();

            return currentValue;
        }

        float FindLongestSide(Model model)
        {
            if (model.Bounds.dimensions.x < model.Bounds.dimensions.z)
            {
                return model.Bounds.dimensions.z;
            }
            else
            {
                return model.Bounds.dimensions.x;
            }
        }

        float Remap(float from, float fromMin, float fromMax, float toMin, float toMax)
        {
            var fromAbs = from - fromMin; var fromMaxAbs = fromMax - fromMin;
            var normal = fromAbs / fromMaxAbs;
            var toMaxAbs = toMax - toMin; var toAbs = toMaxAbs * normal;
            var to = toAbs + toMin;

            return to;
        }
    }
}
