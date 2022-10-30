using StereoKit;
using System;
using System.Collections;
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
        struct SliderValueData
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
            public Vec4[] sliderValue;
        }
        
        Model panel;
        Material panelMaterial;
        Pose panelPose = new Pose(0, 0, -0.4f, Quat.FromAngles(-90, 180, 0));

        ButtonData buttons = new ButtonData();
        HsliderData hSliders = new HsliderData();
        VsliderData vSliders = new VsliderData();
        SliderValueData sliderValues = new SliderValueData();

        Pose node;
        Pose ghostVolumePose;
        Mesh ghostVolume;

        double interval;
        double buttonDelay;
        double interValTime;

        Vec4 buttonAlbedo;
        Vec4 activeColor;
        float buttonRough;
        
        public Dictionary<string, float> buttonStates;

        // Constructor
        public UIElements()
        {
            panelMaterial = new Material(Shader.FromFile("TouchPanelShader.hlsl"));
            panel = Model.FromFile("Panel_v001.glb");
            panelMaterial[MatParamName.DiffuseTex] = panel.Visuals[0].Material.GetTexture("diffuse");
            panelMaterial[MatParamName.MetalTex] = panel.Visuals[0].Material.GetTexture("metal");
            panelMaterial[MatParamName.MetallicAmount] = panel.Visuals[0].Material.GetFloat("metallic");
            panelMaterial[MatParamName.RoughnessAmount] = panel.Visuals[0].Material.GetFloat("roughness");
            panel.Visuals[0].Material = panelMaterial;
            panelMaterial.Transparency = Transparency.Blend;

            ghostVolumePose = new Pose(V.XYZ(0, -0.01f, 0), Quat.FromAngles(90, 0, 0));
            ghostVolume = Mesh.GenerateCube(new Vec3(0.01f, 0.01f, 0.01f));

            buttonStates = new Dictionary<string, float>();

            interval = 0.1d;
            buttonDelay = 0.01d;
            interValTime = Time.Total + interval;

            // Is the panel landscape or portrait?
            float longestSide = FindLongestSide(panel);

            buttons.button = new Vec4[20];
            hSliders.hSlider = new Vec4[10];
            vSliders.vSlider = new Vec4[10];
            sliderValues.sliderValue = new Vec4[20];

            // Parse out the UI elements in the panel gltf file
            var buttonCounter  = 0;
            var hSliderCounter = 0;
            var vSliderCounter = 0;

            foreach (var item in panel.Nodes)
            {
                float _positionX = (item.LocalTransform.Pose.position.x + (panel.Bounds.dimensions.x / 2)) / longestSide;
                float _positionY = (item.LocalTransform.Pose.position.z + (panel.Bounds.dimensions.z / 2)) / longestSide;

                if (item.Name == "panel")
                {
                    var _tempString = item.Info.Get("albedo").Split(new char[] { '[', ']', ',' }, StringSplitOptions.RemoveEmptyEntries);
                    var _r = float.Parse(_tempString[0]);
                    var _g = float.Parse(_tempString[1]);
                    var _b = float.Parse(_tempString[2]);
                    var _a = float.Parse(_tempString[3]);
                    buttonAlbedo = new Vec4(_r, _g, _b, _a);

                    //Console.WriteLine("buttonAlbedo: " + buttonAlbedo.ToString());

                    _tempString = item.Info.Get("activeColor").Split(new char[] { '[', ']', ',' }, StringSplitOptions.RemoveEmptyEntries);
                    _r = float.Parse(_tempString[0]);
                    _g = float.Parse(_tempString[1]);
                    _b = float.Parse(_tempString[2]);

                    var _temp = item.Info.Get("metalness");
                    _a = float.Parse(_temp);

                    activeColor = new Vec4(_r, _g, _b, _a);

                    //Console.WriteLine("activeColor: " + activeColor.ToString());

                    buttonRough = float.Parse(item.Info.Get("roughness"));

                    //Console.WriteLine("roughness: " + buttonRough.ToString());

                }
                else if (item.Info.Get("type") == "button")
                {
                    if (!buttonStates.ContainsKey(item.Info.Get("label")))
                    {
                        buttonStates.Add(item.Info.Get("label"), 0f);
                    }

                    buttons.button[buttonCounter] = new Vec4(_positionX, _positionY, 0, 0);
                    buttonCounter++;
                }
                else if (item.Info.Get("type") == "togglebutton")
                {
                    if (!buttonStates.ContainsKey(item.Info.Get("label")))
                    {
                        buttonStates.Add(item.Info.Get("label"), 0f);
                    }

                    buttons.button[buttonCounter] = new Vec4(_positionX, _positionY, 0, 0);
                    buttonCounter++;
                }
                else if (item.Info.Get("type") == "hslider")
                {
                    hSliders.hSlider[hSliderCounter] = new Vec4(_positionX, _positionY, 0, 0);
                    hSliderCounter++;
                }
                else if (item.Info.Get("type") == "vslider")
                {
                    vSliders.vSlider[vSliderCounter] = new Vec4(_positionX, _positionY, 0, 0);
                    vSliderCounter++;
                }

                // Send UI element setup data to the shader
                panelMaterial.SetInt("buttonAmount", buttonCounter);
                panelMaterial.SetInt("hSliderAmount", hSliderCounter);
                panelMaterial.SetInt("vSliderAmount", vSliderCounter);
                panelMaterial.SetData<ButtonData>("button", buttons);
                panelMaterial.SetData<HsliderData>("hslider", hSliders);
                panelMaterial.SetData<VsliderData>("vslider", vSliders);
                panelMaterial.SetData<SliderValueData>("sliderValue", sliderValues);
                panelMaterial.SetVector("buttonAlbedo", buttonAlbedo);
                panelMaterial.SetVector("activeColor", activeColor);
                panelMaterial.SetFloat("buttonRough", buttonRough);
            }
        }
        
        public void DrawUI(int _no)
        {
            UI.Handle("Panel" + _no, ref panelPose, panel.Bounds);
            panel.Draw(panelPose.ToMatrix());

            Hierarchy.Push(panelPose.ToMatrix());
            var hSliderCounter = 0;
            var vSliderCounter = 9;
            foreach (var item in panel.Nodes)
            {
                if (item.Name != "panel")
                {
                    
                    if (item.Info.Get("type") == "button")
                    {
                        Button(panel, item.Name);
                    }
                    else if (item.Info.Get("type") == "togglebutton")
                    {
                        ToggleButton(panel, item.Name);
                    }
                    else if (item.Info.Get("type") == "hslider")
                    {
                        var _label = item.Info.Get("label");
                        var value = sliderValues.sliderValue[hSliderCounter].x;
                        sliderValues.sliderValue[hSliderCounter].x = HSlider(panel, item.Name, value);
                        buttonStates[_label] = System.Math.Clamp(Remap(value, 0, 0.109f, 1, 0), 0, 1);
                        hSliderCounter++;
                    }
                    else if (item.Info.Get("type") == "vslider")
                    {
                        var _label = item.Info.Get("label");
                        var value = sliderValues.sliderValue[vSliderCounter].x;
                        sliderValues.sliderValue[vSliderCounter].x = VSlider(panel, item.Name, value);
                        buttonStates[_label] = System.Math.Clamp(Remap(value, 0.11f, 0.001f, 0, 1), 0, 1);
                        vSliderCounter++;
                    }
                }
            }
            Hierarchy.Pop();

            if (Time.Total > interValTime)
            {
                // Update the shader with new data derived from button and slider manipulations
                panelMaterial.SetData<SliderValueData>("sliderValue", sliderValues);

                interValTime = Time.Total + interval;
            }
        }

        void Button(Model _model, string _nodeName)
        {
            node = _model.FindNode(_nodeName).ModelTransform.Pose;
            //var _sticky = false;
            var _label = panel.FindNode(_nodeName).Info.Get("label");

            UI.PushSurface(node);
            UI.WindowBegin(_nodeName + "Win", ref ghostVolumePose, UIWin.Empty);
            UI.ButtonBehavior(
                ghostVolume.Bounds.dimensions.XZ.XY0 / 2,
                ghostVolume.Bounds.dimensions.XZ,
                _nodeName,
                out float finger,
                out BtnState state,
                out BtnState focus);
            UI.WindowEnd();
            UI.PopSurface();

            if ((state & BtnState.JustActive) > 0)
            {
                interValTime = Time.Total + buttonDelay;
                buttonStates[_label] = 1;
            }
            
            if (buttonStates[_label] == 1 & Time.Total > interValTime)
            {
                buttonStates[_label] = 0;
                interValTime += buttonDelay;
            }
        }

        void ToggleButton(Model _model, string _nodeName)
        {
            node = _model.FindNode(_nodeName).ModelTransform.Pose;
            //var _sticky = false;
            var _label = panel.FindNode(_nodeName).Info.Get("label");

            UI.PushSurface(node);
            UI.WindowBegin(_nodeName + "Win", ref ghostVolumePose, UIWin.Empty);
            UI.ButtonBehavior(
                ghostVolume.Bounds.dimensions.XZ.XY0 / 2,
                ghostVolume.Bounds.dimensions.XZ,
                _nodeName,
                out float finger,
                out BtnState state,
                out BtnState focus);
            UI.WindowEnd();
            UI.PopSurface();

            if ((state & BtnState.JustActive) > 0)
            {
                buttonStates[_label] = buttonStates[_label] == 1 ? 0 : 1;
                interValTime = Time.Total + buttonDelay;
            }
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
