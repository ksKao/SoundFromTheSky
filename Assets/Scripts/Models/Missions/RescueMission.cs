using UnityEngine.UIElements;

public class RescueMission : Mission
{
    private TrainSO _train;
    private int _numberOfSupplies = 0;
    private int _numberOfCrews = 0;

    public override MissionType Type => MissionType.Rescue;

    public RescueMission() : base()
    {
        _train = DataManager.Instance.GetRandomTrain();
        _routeStartLocation = _train.routeStartLocation;
        _routeEndLocation = _train.routeEndLocation;
    }

    public override VisualElement GenerateMissionUI()
    {
        VisualElement root = new();

        root.Add(new Label(Type.ToString()));

        return root;
    }
}
