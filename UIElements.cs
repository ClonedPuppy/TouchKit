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
        struct SliderData
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
            public Vec4[] slider;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct SliderRangeData
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
            public Vec4[] sliderRange;
        }

        Pose cubePose = new Pose(0, 0, -0.4f, Quat.FromAngles(-90, 180, 0));
        Model touchPanel;
        Tex touchPanelDiff;
        Tex touchPanelMRAO;
        Shader touchPanelShader;
        public static Material touchPanelMat;

        List<Vec4> buttonList = new List<Vec4>();
        List<Vec4> sliderList = new List<Vec4>();
        List<Vec4> sliderRangeList = new List<Vec4>();

        ButtonData buttons = new ButtonData();
        SliderData sliders = new SliderData();
        SliderRangeData sliderRanges = new SliderRangeData();

        Bounds buttonBounds;
        Pose node;
        Pose PoseNeutral;
        Vec3 size;
        Mesh button;
        Material buttonMat;
        
        public static Dictionary<string, bool> buttonStates;
        public static Dictionary<string, float> sliderStates;

        double interval;
        double interValTime;

        // Constructor
        public UIElements()
        {
            touchPanel = Model.FromFile("Panel_v001.glb");
            touchPanelDiff = Tex.FromFile("TouchMenuDiffuse.png");
            touchPanelMRAO = Tex.FromFile("TouchMenuMRAO.png");
            touchPanelShader = Shader.FromFile("TouchPanelShader.hlsl");
            touchPanelMat = new Material(touchPanelShader);
            touchPanelMat[MatParamName.DiffuseTex] = touchPanelDiff;
            touchPanelMat[MatParamName.MetalTex] = touchPanelMRAO;
            touchPanelMat.Transparency = Transparency.Blend;
            touchPanel.Visuals[0].Material = touchPanelMat;

            size = new Vec3(0.02f, 0.02f, 0.02f);
            PoseNeutral = new Pose(V.XYZ(0, -0.01f, 0), Quat.FromAngles(90, 0, 0));
            buttonBounds = new Bounds(size);
            button = Mesh.GenerateCube(size);
            buttonMat = Default.MaterialUnlit;
            buttonStates = new Dictionary<string, bool>();
            sliderStates = new Dictionary<string, float>();
            interval = 0.3d;
            interValTime = Time.Total + interval;

            // Is the panel landscape or portrait?
            float longestSide = FindLongestSide(touchPanel);

            buttons.button = new Vec4[20];
            sliders.slider = new Vec4[20];

            sliderRanges.sliderRange = new Vec4[20];

            // Parse out buttons and sliders in the gltf file
            var i = 0;
            var j = 0;
            foreach (var item in touchPanel.Nodes)
            {
                if (item.Name.Contains("Button"))
                {
                    float _positionX = (item.LocalTransform.Pose.position.x + (touchPanel.Bounds.dimensions.x / 2)) / longestSide;
                    float _positionY = (item.LocalTransform.Pose.position.z + (touchPanel.Bounds.dimensions.z / 2)) / longestSide;

                    float metallic = 0;
                    float roughness = 0;
                    if (item.Info.Count > 0)
                    {
                        if (item.Info.Contains("metallic"))
                        {
                            metallic = float.Parse(item.Info["metallic"]);
                        }
                        if (item.Info.Contains("roughness"))
                        {
                            roughness = float.Parse(item.Info["roughness"]);
                        }
                    }

                    buttons.button[i] = new Vec4(_positionX, _positionY, metallic, roughness);
                    buttonList.Add(new Vec4(_positionX, _positionY, metallic, roughness));
                    i++;
                }
                else if (item.Name.Contains("Slider"))
                {
                    float _positionX = (item.LocalTransform.Pose.position.x + (touchPanel.Bounds.dimensions.x / 2)) / longestSide;
                    float _positionY = (item.LocalTransform.Pose.position.z + (touchPanel.Bounds.dimensions.z / 2)) / longestSide;

                    float metallic = 0;
                    float roughness = 0;
                    if (item.Info.Count > 0)
                    {
                        if (item.Info.Contains("metallic"))
                        {
                            metallic = float.Parse(item.Info["metallic"]);
                        }
                        if (item.Info.Contains("roughness"))
                        {
                            roughness = float.Parse(item.Info["roughness"]);
                        }
                    }

                    sliders.slider[j] = new Vec4(_positionX, _positionY, metallic, roughness);
                    sliderList.Add(new Vec4(_positionX, _positionY, metallic, roughness));
                    j++;
                }
                // Send UI element setup data to the shader
                touchPanelMat.SetInt("buttonAmount", i);
                touchPanelMat.SetInt("sliderAmount", j);
                touchPanelMat.SetData<ButtonData>("button", buttons);
                touchPanelMat.SetData<SliderData>("slider", sliders);
                touchPanelMat.SetData<SliderRangeData>("sliderRange", sliderRanges);
            }
        }

        public void DrawUI()
        {
            UI.Handle("Cube", ref cubePose, touchPanel.Bounds);
            touchPanel.Draw(cubePose.ToMatrix());

            Hierarchy.Push(cubePose.ToMatrix());
            var i = 0;
            foreach (var item in touchPanel.Nodes)
            {
                if (item.Name.Contains("Button"))
                {
                    Button(touchPanel, item.Name, false);
                }
                else if (item.Name.Contains("Slider"))
                {
                    var value = sliderRanges.sliderRange[i].x;
                    sliderRanges.sliderRange[i].x = Slider(touchPanel, item.Name, false, value);
                    i++;
                }
            }
            Hierarchy.Pop();

            // Update the shader with new data derived from button and slider manipulations
            touchPanelMat.SetData<SliderRangeData>("sliderRange", sliderRanges);

        }

        void Button(Model _model, string _nodeName, bool _sticky)
        {
            if (!buttonStates.ContainsKey(_nodeName))
            {
                buttonStates.Add(_nodeName, false);
            }

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
                buttonStates[_nodeName] = buttonStates[_nodeName] == true ? false : true;
                interValTime = Time.Total + interval;
                if (buttonStates[_nodeName] == true)
                {
                    //System.Console.WriteLine(_nodeName.ToString() + " Pressed");
                    //Assets.surfaceTopMat.SetFloat(_nodeName, 1);
                    //if (nodeName == "Play")
                    //{
                    //    buttonStates["Stop"] = false;
                    //    Assets.surfaceTopMat.SetFloat("Stop", 0);
                    //}
                    //else
                    //{
                    //    buttonStates["Play"] = false;
                    //    Assets.surfaceTopMat.SetFloat("Play", 0);
                    //}
                }
                else
                {
                    //Assets.surfaceTopMat.SetFloat(_nodeName, 0);
                }
            }

            //if (!_sticky & buttonStates[_nodeName] == true & Time.Total > interValTime)
            //{
            //    buttonStates[_nodeName] = false;
            //    //Assets.surfaceTopMat.SetFloat(_nodeName, 0);
            //    interValTime += interval;
            //}
        }

        float Slider(Model _model, string _nodeName, bool _sticky, float currentValue)
        {
            if (!sliderStates.ContainsKey(_nodeName))
            {
                sliderStates.Add(_nodeName, 0.5f);
            }

            node = _model.FindNode(_nodeName).ModelTransform.Pose;

            UI.PushSurface(node);
            Vec3 volumeAt = new Vec3(0, 0, 0);
            Vec3 volumeSize = new Vec3(0.065f, 0.02f, 0.02f);

            BtnState volumeState = UI.VolumeAt(_nodeName + "Volume", new Bounds(volumeAt, volumeSize), UIConfirm.Push, out Handed hand);
            if (volumeState != BtnState.Inactive)
            {
                var result = System.Math.Clamp(Remap(Hierarchy.ToLocal(Input.Hand(hand)[FingerId.Index, JointId.Tip].Pose).position.x, -0.03f, 0.028f, 0.1f, 0.001f), 0, 0.2f);
                sliderStates[_nodeName] = System.Math.Clamp(Remap(Hierarchy.ToLocal(Input.Hand(hand)[FingerId.Index, JointId.Tip].Pose).position.x, -0.03f, 0.028f, 0f, 1f), 0, 1f);
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
