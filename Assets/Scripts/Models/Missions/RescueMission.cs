using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Diagnostics;
using UnityEngine.UIElements;

public class RescueMission : Mission
{
    private const int MILES_PER_PASSENGER_INCREASE = 5;
    private readonly NumberInput _supplyNumberInput = new("Supply");
    private readonly NumberInput _crewNumberInput = new("Crew");
    private readonly double _passengerIncreaseProbability = 0.5f; // determines the probability that the train will have 1 more passenger (50% base chance)
    private readonly RescueMissionResolvePanel _rescueMissionResolvePanel = null;
    private bool _actionTakenDuringThisEvent = false;

    public override Route Route => new(Train.routeStartLocation, Train.routeEndLocation);
    public override MissionType Type { get; } = MissionType.Rescue;
    public List<Passenger> Passengers { get; } = new();
    public int NumberOfSupplies { get; private set; } = 0;
    public int NumberOfCrews { get; private set; } = 0;
    public bool ActionTakenDuringThisEvent
    {
        get => _actionTakenDuringThisEvent;
        set
        {
            _actionTakenDuringThisEvent = value;

            // cannot ignore event if already used supply/crew on passenger
            _rescueMissionResolvePanel.ignoreButton.visible = !value;
        }
    }

    public RescueMission() : base()
    {
        int weatherIndex = Array.IndexOf(DataManager.Instance.AllWeathers, weather);
        _passengerIncreaseProbability += weatherIndex * 0.05; // each weather difficulty will additionally increase the probability to get a passenger by 5%
        _rescueMissionResolvePanel = new(this);
    }

    public override bool Deploy()
    {
        // check if this train has already been deployed
        if (GameManager.Instance.deployedMissions.Any(m => m.Train != null && m.Train.name == Train.name))
            return false;

        NumberOfSupplies = _supplyNumberInput.Value;
        NumberOfCrews = _crewNumberInput.Value;

        return true;
    }

    public override void GenerateDeployedMissionUi()
    {
        base.GenerateDeployedMissionUi();
    }

    public void UseSupply()
    {
        Passenger[] selectedPassengers = Passengers.Where(p => p.Selected).ToArray();

        if (selectedPassengers.Length == 0)
        {
            Debug.Log("No passengers selected");
            return;
        }

        if (NumberOfSupplies < selectedPassengers.Length)
        {
            Debug.Log("Not enough supplies.");
            return;
        }

        foreach (Passenger passenger in selectedPassengers)
        {
            passenger.MakeBetter();
            passenger.Selected = false;
        }

        NumberOfSupplies -= selectedPassengers.Length;
        _rescueMissionResolvePanel.RefreshButtonText();

        ActionTakenDuringThisEvent = true;
    }

    public void UseCrew()
    {
        Passenger[] selectedPassengers = Passengers.Where(p => p.Selected).ToArray();

        if (selectedPassengers.Length == 0)
        {
            Debug.Log("No passengers selected");
            return;
        }

        if (NumberOfCrews < selectedPassengers.Length)
        {
            Debug.Log("Not enough crews.");
            return;
        }

        foreach (Passenger passenger in selectedPassengers)
        {
            // the chance of passenger's health decreasing is same as the weather event occur probability
            if (Random.ShouldOccur(weather.decisionMakingProbability))
                passenger.MakeWorse();
            else
                passenger.MakeBetter();
            passenger.Selected = false;
        }

        NumberOfCrews -= selectedPassengers.Length;
        _rescueMissionResolvePanel.RefreshButtonText();
        ActionTakenDuringThisEvent = true;
    }

    public void Ignore()
    {
        // technically should not be possible to reach this condition, but add here just in case if this method is called by something other than the on click on Ignore button
        if (ActionTakenDuringThisEvent)
        {
            Debug.Log("Cannot ignore event after using supply or crew.");
            return;
        }

        EventPending = false;

        foreach (Passenger passenger in Passengers)
            passenger.Selected = false;

        ActionTakenDuringThisEvent = false;
        UiManager.Instance.GameplayScreen.ChangeRightPanel(UiManager.Instance.GameplayScreen.deployedMissionList);
    }

    public void Finish()
    {
        // if didn't use supply or crew, then this event remains unresolved.
        if (!ActionTakenDuringThisEvent)
        {
            Debug.Log("Cannot finish resolve event without first using supply or crew.");
            return;
        }

        ActionTakenDuringThisEvent = false;
        UiManager.Instance.GameplayScreen.ChangeRightPanel(UiManager.Instance.GameplayScreen.deployedMissionList);
        EventPending = false;
    }

    public override void OnResolveButtonClicked()
    {
        // cannot call base here since need to wait until player make a decision before continuing
        _rescueMissionResolvePanel.RegenerateDeployedMissionUi();
        UiManager.Instance.GameplayScreen.ChangeRightPanel(_rescueMissionResolvePanel);
    }

    protected override void OnMileChange()
    {
        base.OnMileChange();

        if (IsMilestoneReached(MILES_PER_PASSENGER_INCREASE) && Random.ShouldOccur(_passengerIncreaseProbability))
            Passengers.Add(new());
    }

    protected override void EventOccur()
    {
        foreach (Passenger passenger in Passengers)
        {
            // 50% chance for a passenger health to change
            if (Random.ShouldOccur(0.5))
            {
                // 50% to increase health, 50% to decrease health
                if (Random.ShouldOccur(0.5))
                    passenger.MakeWorse();
                else
                    passenger.MakeBetter();
            }
        }

        // remove all dead passengers
        Passengers.RemoveAll(p => p.Status == PassengerStatus.Death);

        // if there are no passengers, can set event pending back to false
        // this can happen in the very early stage of the mission, where the 50% chance of getting a new passenger does not occur
        // which can cause confusion as there are no passengers but there is still an event
        // also check for if all passengers have status of comfortable, otherwise there is no reason for an event to happen
        if (Passengers.Count == 0 || Passengers.All(p => p.Status == PassengerStatus.Comfortable))
            EventPending = false;
    }

    protected override void GeneratePendingMissionUi()
    {
        base.GeneratePendingMissionUi();

        PendingMissionUi.Add(new Label(Train.name));

        PendingMissionUi.Add(_supplyNumberInput);
        PendingMissionUi.Add(_crewNumberInput);
    }
}
