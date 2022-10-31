using StereoKit;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace TouchMenuApp
{
    class UIElements
    {
        // Declare structs to be sent to shader
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

        // Declare variables
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
        string panelName;

        public Dictionary<string, float> buttonStates;

        // Constructor, requires a panel name, remember the gltf and hlsl files in /Assets need to use this name as well
        public UIElements(string _panelName)
        {
            panelName = _panelName;

            // Load the panel model and material
            panelMaterial = new Material(Shader.FromFile(panelName + ".hlsl"));
            panel = Model.FromFile(panelName + "_v001.glb");

            // Transfer material parameters from loaded model to the custom material
            panelMaterial[MatParamName.DiffuseTex] = panel.Visuals[0].Material.GetTexture("diffuse");
            panelMaterial[MatParamName.OcclusionTex] = panel.Visuals[0].Material.GetTexture("occlusion");
            panelMaterial[MatParamName.MetalTex] = panel.Visuals[0].Material.GetTexture("metal");
            panelMaterial[MatParamName.ColorTint] = panel.Visuals[0].Material.GetVector4("color");
            panelMaterial[MatParamName.MetallicAmount] = panel.Visuals[0].Material.GetFloat("metallic");
            panelMaterial[MatParamName.RoughnessAmount] = panel.Visuals[0].Material.GetFloat("roughness");
            panel.Visuals[0].Material = panelMaterial;
            panelMaterial.Transparency = Transparency.Blend;

            // A invisible mesh to be used as a manipulation volume for the various UI elements
            ghostVolumePose = new Pose(V.XYZ(0, -0.01f, 0), Quat.FromAngles(90, 0, 0));
            ghostVolume = Mesh.GenerateCube(new Vec3(0.01f, 0.01f, 0.01f));

            // Holds all the current button states
            buttonStates = new Dictionary<string, float>();

            // Timer stuff
            interval = 0.1d;
            buttonDelay = 0.01d;
            interValTime = Time.Total + interval;

            // Check if the panel is a landscape or portrait aspect
            float longestSide = FindLongestSide(panel);

            // Initialize the arrays with default values (increase these if more buttons/sliders are needed)
            buttons.button = new Vec4[20];
            hSliders.hSlider = new Vec4[10];
            vSliders.vSlider = new Vec4[10];
            sliderValues.sliderValue = new Vec4[20];

            // Parse out the UI elements embedded in the specified gltf file
            var buttonCounter = 0;
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

                    _tempString = item.Info.Get("activeColor").Split(new char[] { '[', ']', ',' }, StringSplitOptions.RemoveEmptyEntries);
                    _r = float.Parse(_tempString[0]);
                    _g = float.Parse(_tempString[1]);
                    _b = float.Parse(_tempString[2]);

                    var _temp = item.Info.Get("metalness");
                    _a = float.Parse(_temp);

                    activeColor = new Vec4(_r, _g, _b, _a);

                    buttonRough = float.Parse(item.Info.Get("roughness"));
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
                    var defValue = float.Parse(item.Info.Get("defState"));
                    hSliders.hSlider[hSliderCounter] = new Vec4(_positionX, _positionY, 0, 0);
                    sliderValues.sliderValue[hSliderCounter] = new Vec4(defValue, 0, 0, 0);
                    hSliderCounter++;
                }
                else if (item.Info.Get("type") == "vslider")
                {
                    var defValue = float.Parse(item.Info.Get("defState"));
                    vSliders.vSlider[vSliderCounter] = new Vec4(_positionX, _positionY, 0, 0);
                    sliderValues.sliderValue[vSliderCounter + 9] = new Vec4(defValue, 0, 0, 0);
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

            for (int i = 0; i < sliderValues.sliderValue.Length; i++)
            {
                Console.WriteLine(sliderValues.sliderValue[i].x.ToString());
            }
        }

        public void DrawUI()
        {
            // Draw the panel
            UI.Handle(panelName + "Panel", ref panelPose, panel.Bounds);
            panel.Draw(panelPose.ToMatrix());

            // Now draw the UI elements
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
                        buttonStates[_label] = value;
                        hSliderCounter++;
                    }
                    else if (item.Info.Get("type") == "vslider")
                    {
                        var _label = item.Info.Get("label");
                        var value = sliderValues.sliderValue[vSliderCounter].x;
                        sliderValues.sliderValue[vSliderCounter].x = VSlider(panel, item.Name, value);
                        buttonStates[_label] = value;
                        vSliderCounter++;
                    }
                }
            }
            Hierarchy.Pop();

            // Send the UI element state data to the shader, it's set to do this at intervals to reduce the amount of data being sent
            // Change the interval if faster / slower is prefered.
            if (Time.Total > interValTime)
            {
                panelMaterial.SetData<SliderValueData>("sliderValue", sliderValues);
                interValTime = Time.Total + interval;
            }
        }

        // Momentary button
        void Button(Model _model, string _nodeName)
        {
            node = _model.FindNode(_nodeName).ModelTransform.Pose;
            var _label = panel.FindNode(_nodeName).Info.Get("label");

            UI.PushSurface(node);
            UI.WindowBegin(_nodeName + panelName + "Win", ref ghostVolumePose, UIWin.Empty);
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

        // Toggle button
        void ToggleButton(Model _model, string _nodeName)
        {
            node = _model.FindNode(_nodeName).ModelTransform.Pose;
            var _label = panel.FindNode(_nodeName).Info.Get("label");

            UI.PushSurface(node);
            UI.WindowBegin(_nodeName + panelName + "Win", ref ghostVolumePose, UIWin.Empty);
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

        // Horizontal slider
        float HSlider(Model _model, string _nodeName, float currentValue)
        {
            node = _model.FindNode(_nodeName).ModelTransform.Pose;

            UI.PushSurface(node);
            Vec3 volumeAt = new Vec3(0, 0, 0);
            Vec3 volumeSize = new Vec3(0.065f, 0.01f, 0.01f);

            BtnState volumeState = UI.VolumeAt(_nodeName + panelName + "HVolume", new Bounds(volumeAt, volumeSize), UIConfirm.Push, out Handed hand);
            if (volumeState != BtnState.Inactive)
            {
                var result = System.Math.Clamp((Hierarchy.ToLocal(Input.Hand(hand)[FingerId.Index, JointId.Tip].Pose).position.x + 0.03f) * 16f, 0f, 1f);
                UI.PopSurface();
                return result;
            }
            UI.PopSurface();

            return currentValue;
        }

        // Vertical slider
        float VSlider(Model _model, string _nodeName, float currentValue)
        {
            node = _model.FindNode(_nodeName).ModelTransform.Pose;

            UI.PushSurface(node);
            Vec3 volumeAt = new Vec3(0, 0, 0);
            Vec3 volumeSize = new Vec3(0.01f, 0.01f, 0.065f);
            
            BtnState volumeState = UI.VolumeAt(_nodeName + panelName + "VVolume", new Bounds(volumeAt, volumeSize), UIConfirm.Push, out Handed hand);
            if (volumeState != BtnState.Inactive)
            {
                var result = System.Math.Clamp((Hierarchy.ToLocal(Input.Hand(hand)[FingerId.Index, JointId.Tip].Pose).position.z - 0.03f) * -16f, 0f, 1f);
                UI.PopSurface();
                return result;
            }
            UI.PopSurface();

            return currentValue;
        }

        // Function to find the aspect of the panel model
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

        // Function to remap a value from one range to another
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
