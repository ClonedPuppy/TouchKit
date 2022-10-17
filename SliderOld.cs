using StereoKit;

namespace TouchMenuApp
{
    class SliderOld
    {
        void Draw()
        {
            // Draw a transparent volume so the user can see this space
            Vec3 volumeAt = new Vec3(0, 0.2f, -0.4f);
            float volumeSize = 0.2f;
            Default.MeshCube.Draw(Default.MaterialUIBox, Matrix.TS(volumeAt, volumeSize));

            BtnState volumeState = UI.VolumeAt("Volume", new Bounds(volumeAt, Vec3.One * volumeSize), UIConfirm.Pinch, out Handed hand);
            if (volumeState != BtnState.Inactive)
            {
                // If it just changed interaction state, make it jump in size
                float scale = volumeState.IsChanged()
                    ? 0.1f
                    : 0.05f;
                Lines.AddAxis(Input.Hand(hand)[FingerId.Index, JointId.Tip].Pose, scale);
            }
        }
    }

}