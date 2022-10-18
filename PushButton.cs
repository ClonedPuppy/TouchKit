using StereoKit;
using System;
using System.Collections.Generic;

namespace TouchMenuApp
{
    class PushButton
    {
        Bounds buttonBounds;
        Pose node;
        Pose PoseNeutral;
        Vec3 size;
        Mesh button;
        Material buttonMat;
        Dictionary<string, bool> buttonStates;

        double interval;
        double interValTime;

        public PushButton()
        {
            size = new Vec3(0.02f, 0.02f, 0.02f);
            PoseNeutral = new Pose(V.XYZ(0, -0.01f, 0), Quat.FromAngles(90, 0, 0));
            buttonBounds = new Bounds(size);
            button = Mesh.GenerateCube(size);
            buttonMat = Default.MaterialUnlit;
            buttonStates = new Dictionary<string, bool>();
            interval = 0.3d;
            interValTime = Time.Total + interval;
        }

        public void Button(Model _model, string _nodeName, bool _sticky)
        {
            if (!buttonStates.ContainsKey(_nodeName))
            {
                buttonStates.Add(_nodeName, false);
            }

            node = _model.FindNode(_nodeName).ModelTransform.Pose;

            //UI.ShowVolumes = true;
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
            //UI.ShowVolumes = false;

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

            if (!_sticky & buttonStates[_nodeName] == true & Time.Total > interValTime)
            {
                buttonStates[_nodeName] = false;
                //Assets.surfaceTopMat.SetFloat(_nodeName, 0);
                interValTime += interval;
            }
        }

        public void Slider(Model _model, string _nodeName, bool _sticky)
        {
            if (!buttonStates.ContainsKey(_nodeName))
            {
                buttonStates.Add(_nodeName, false);
            }

            node = _model.FindNode(_nodeName).ModelTransform.Pose;

            UI.ShowVolumes = true;
            UI.PushSurface(node);
            Vec3 volumeAt = new Vec3(0, 0, 0);
            Vec3 volumeSize = new Vec3(0.065f, 0.02f, 0.02f);
            //Default.MeshCube.Draw(Default.Material, Matrix.TS(volumeAt, volumeSize));
            UI.ShowVolumes = false;

            BtnState volumeState = UI.VolumeAt("Volume", new Bounds(volumeAt, volumeSize), UIConfirm.Push, out Handed hand);
            if (volumeState != BtnState.Inactive)
            {
                App.touchPanelMat.SetFloat("sliderRange", Math.Clamp(Remap(Input.Hand(hand)[FingerId.Index, JointId.Tip].Pose.position.x, 0, 0.13f, 0.2f, 0), 0, 02f));
            }
            UI.PopSurface();
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