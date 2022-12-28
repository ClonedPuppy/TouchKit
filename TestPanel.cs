using StereoKit;

namespace TouchMenuApp
{
    class TestPanel
    {
        Model testPanel;
        Pose testPanelPose;
        TextStyle testPanelTextStyle;

        public TestPanel()
        {
            testPanel = new Model(Mesh.GenerateRoundedCube(V.XYZ(4, 2, 0.1f), 0.05f), Material.Default);
            testPanelPose = new Pose(V.XYZ(0, 0, -3), Quat.FromAngles(0, 180, 0));

            testPanelTextStyle = Text.MakeStyle(Default.Font, 8 * U.cm, Color.HSV(0.55f, 0.62f, 0.93f));
        }

        public void DrawTestPanel()
        {
            Hierarchy.Push(testPanelPose.ToMatrix());
            testPanel.Draw(Matrix.Identity);

            var testLabels = 0;
            var xPos = 1.5f;
            var yPos = 0.7f;

            foreach (var pair in UIElements.buttonStates)
            {
                Text.Add(pair.Key + ": " + pair.Value.ToString("n1"), Matrix.T(V.XYZ(xPos, yPos, -0.06f)), testPanelTextStyle);
                xPos = xPos - 1f;
                testLabels++;

                if (testLabels == 4 | testLabels == 8 | testLabels == 12 | testLabels == 16)
                {
                    xPos = 1.5f;
                    yPos = yPos - 0.35f;
                }
            }
            Hierarchy.Pop();
        }
    }
}