﻿// THIS SCRIPT CONTROLS THE MOVEMENT OF THE PIN (HAND) OF THE GAUGE AND SHOULD BE ATTACHED IT.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public struct Destination
{
    public string name;
    public float minPosition;
    public float maxPosition;
}

public class GaugeIndicator : MonoBehaviour
{
    // [Non-Editor Variables]

    private int _heatIndex, _firstHeatIndex, _secondHeatIndex, _initialHeatIndex, _startHeatIndex, _finalHeatIndex;
    private int _startCoolIndex, _coolIndex, _finalCoolIndex;
    private float _firstHeatPoint, _initialHeatPoint, _finalHeatPoint, _firstCoolPoint, _finalCoolPoint;
    private float _nextRotationPoint, _prevRotationPoint;
    private float _rotationIncrement, _smallIncrement, _smallDelay;
    private Quaternion _defaultRotation, _finalRotation, _descendRotation;
    private bool _didForwardRot = false, _didBackRot = false;
    private int _fireCalls, _iceCalls;

    // [Editor Variables]

    [Header("<size=15>Rotation Parameters</size>")]
    [Space]
    [SerializeField] private Vector3 _rotationAxis = Vector3.back;
    [SerializeField][Range(0.0f, 360.0f)] private float _minDegreesBelowStart = 90.0f;
    [Range(0, 100)] public int _firePercentage = 40;
    [Range(0, 100)] public int _icePercentage = 20;
    [SerializeField][Range(19.9f, 360.0f)] private float _maxDegrees = 180.0f;
    [SerializeField][Range(0.0f, 100.0f)] private float _heatingSpeed = 40.0f;
    [SerializeField][Range(0.0f, 100.0f)] private float _coolingSpeed = 40.0f;

    [Header("<size=15>Location Parameters</size>")]
    [Space]
    [SerializeField]
    public Destination[] _destinations =
    {
        new Destination { name = "On", minPosition = 0.0f, maxPosition = 0.0f},
    };
    public bool MoveToNextPoint = false;
    public bool MoveToPrevPoint = false;
    public bool ResetPinLocation = false;
    public bool FinalPinLocation = false;

    [Header("<size=14>Test-Only Rotation Parameters</size>")]
    [Space]
    [SerializeField] private float[] _heatRotationPoints = { 0.0f, 70f, 140.0f, 210.0f };
    [SerializeField] private float[] _coolRotationPoints = { 0.0f, 35.0f, 70.0f, 105.0f, 140.0f, 175.0f, 210.0f };
    [SerializeField][Range(0, 50)] private int _equalHeatPoints = 3;
    [SerializeField][Range(10.0f, 360.0f)] private float _equalHeatEndPoint = 210.0f;
    [Space]
    [SerializeField][Range(0, 50)] private int _equalCoolPoints = 7;
    [SerializeField][Range(10.0f, 360.0f)] private float _equalCoolEndPoint = 210.0f;
    [Space]
    [SerializeField][Range(0.0f, 100.0f)] private float _autoCoolDelay = 2.0f;
    public bool AutoCoolOn = false;
    public bool AutoCoolTriggerOn = false;
    public bool InstantRotation = false;
    public bool RunTimeReposition = false;
    public bool HeatOnlyScale = false;
    [Range(0, 50)] public int FireCalls = 1;
    [Range(0, 50)] public int IceCalls = 1;

    [Header("<size=14>Test-Only Location Parameters </size>")]
    [Space]
    [SerializeField] private bool _setStartPosition = false;
    [SerializeField] private Vector3 _startPosition = Vector3.zero;
    [Space]
    [SerializeField] private bool _setStartRotation = false;
    [SerializeField] private Vector3 _startRotation = Vector3.zero;
    [Space]
    [SerializeField] private bool _minLocation = false;

    public UnityEvent<string> OnComplete;

    // [Events]

    private void Awake()
    {
        // Sets Number of Calls to Repeat
        SetFireCalls();
        SetIceCalls();

        // Sets Equal Points to rotate to.
        SetEqualHeatPoints();
        SetEqualCoolPoints();

        // Local Start Co-ordinates override.
        if (_setStartPosition)
        {
            transform.localPosition = _startPosition;
        }
        if (_setStartRotation)
        {
            transform.localRotation = Quaternion.Euler(_startRotation);
        }

        // Indexes of Arrays.
        _firstHeatIndex = 0;
        _startCoolIndex = 0;
        _secondHeatIndex = _firstHeatIndex + 1;
        _startHeatIndex = GetStartIndex();
        _initialHeatIndex = _startHeatIndex - 1;
        _finalHeatIndex = _heatRotationPoints.Length - 1;
        _finalCoolIndex = _coolRotationPoints.Length - 1;
        _heatIndex = _startHeatIndex;
        _coolIndex = FindCoolIndex(_heatIndex);

        // Elements of Array
        _firstHeatPoint = _heatRotationPoints[_firstHeatIndex];
        _firstCoolPoint = _coolRotationPoints[_startCoolIndex];
        _initialHeatPoint = _heatRotationPoints[_initialHeatIndex];
        _finalHeatPoint = _heatRotationPoints[_finalHeatIndex];
        _finalCoolPoint = _coolRotationPoints[_finalCoolIndex];
        _nextRotationPoint = _heatRotationPoints[_heatIndex];
        _prevRotationPoint = !HeatOnlyScale ? _firstCoolPoint : _firstHeatPoint;

        // Rotations CHECK THIS
        _defaultRotation = transform.rotation;
        _finalRotation = _defaultRotation * Quaternion.Euler(_rotationAxis * _finalHeatPoint);
        _descendRotation = _defaultRotation * Quaternion.Euler(_rotationAxis * _firstCoolPoint);

        // Corrective Variables
        _smallIncrement = 0.2f;
        _smallDelay = 0.1f;
    }

    private void Update()
    {
        if (ResetPinLocation)
        {
            ResetPinLocation = false;
            ResetGauge();
        }
        else if (FinalPinLocation)
        {
            FinalPinLocation = false;
            MaxGauge();
        }
        else if (_minLocation)
        {
            _minLocation = false;
            if (!HeatOnlyScale)
            {
                MinGauge();
            }
        }
        else if (MoveToNextPoint && FireCalls != 0) // Determines if forward rotation is enabled.
        {
            // Cancels if at the end.
            if (transform.rotation == _finalRotation && _nextRotationPoint == _finalHeatPoint)
            {
                MoveToNextPoint = false;
                return;
            }
            MoveToPrevPoint = false;
            IncrementGauge();

        }
        else if (AutoCoolOn || (MoveToPrevPoint && IceCalls != 0)) // Cools down (reverse rotation) to origin if not.
        {
            // Cancels if at the start.
            if ((HeatOnlyScale && transform.rotation == _defaultRotation && _prevRotationPoint == _firstHeatPoint) || (transform.rotation == _descendRotation && _prevRotationPoint == _firstCoolPoint))
            {
                MoveToPrevPoint = false;
                AutoCoolOn = false;
                return;
            }

            MoveToNextPoint = false;
            DecrementGauge();
        }
    }

    // [Custom Methods]

    public void IncrementGauge()
    {
        if (_heatIndex <= _finalHeatIndex) // Determines if there is a valid point to rotate to.
        {
            float sign = Mathf.Sign(_nextRotationPoint - _prevRotationPoint);
            const float pos = 1.0f, neg = -1.0f;

            bool incPosRotation = sign == pos && _rotationIncrement < _nextRotationPoint;
            bool incNegRotation = sign == neg && _rotationIncrement > _nextRotationPoint;

            // Greater than symbol is necessary because of imprecision.
            bool completePosRotation = sign == pos && _rotationIncrement >= _nextRotationPoint;
            bool completeNegRotation = sign == neg && _rotationIncrement <= _nextRotationPoint;
            if (incPosRotation || incNegRotation)
            {
                if (!InstantRotation)
                {
                    transform.Rotate(_rotationAxis, _heatingSpeed * Time.deltaTime * sign);
                    _rotationIncrement += _heatingSpeed * Time.deltaTime * sign;

                    CheckForDestination();

                    // corrects rotation to end point. Needs to be this long because it won't get assigned quick enough in a local variable.
                    if (_nextRotationPoint == _finalHeatPoint && ((sign == pos && _rotationIncrement > _finalHeatPoint - _smallIncrement) || (sign == neg && _rotationIncrement < _finalHeatPoint + _smallIncrement)))
                    {
                        Invoke("MaxGauge", _smallDelay);
                    }
                }
                else
                {
                    JumpToNextRotationPoint();

                    // corrects rotation to end point.
                    if (_nextRotationPoint == _finalHeatPoint)
                    {
                        Invoke("MaxGauge", _smallDelay);
                    }
                }
                _didForwardRot = true;
            }

            // When the point is reached, rotation is disabled, and the next point (to rotate to) is assigned.

            else if (completePosRotation || completeNegRotation)
            {
                JumpToNextRotationPoint(); // corrects the imprecision

                // Previous backward rotation check.
                if (!_didBackRot || !HeatOnlyScale)
                {
                    MoveToNextPoint = false;
                }
                else
                {
                    _didBackRot = false;
                }

                if (RunTimeReposition)
                {
                    UpdateRotationPoints(IncrementStatus.Forward);
                }

                _heatIndex += (_heatIndex < _finalHeatIndex) ? 1 : 0; // increases _heatIndex unless end.
                SetRotationPoints(_heatIndex, _coolIndex);

                AutoCoolOn = false;

                CheckForDestination();

                if (AutoCoolTriggerOn)
                {
                    Invoke("AwakenCoolDown", _autoCoolDelay);
                }

                // Repeated Increment calls.

                if (_fireCalls > 0)
                {
                    _fireCalls--;
                    MoveToNextPoint = true;
                }
                else
                {
                    Invoke("SetFireCalls", _smallDelay);
                }
            }
        }
    }

    public void DecrementGauge()
    {
        float sign = Mathf.Sign(_prevRotationPoint - _nextRotationPoint);
        const float pos = 1.0f, neg = -1.0f;

        bool decPosRotation = sign == pos && _rotationIncrement < _prevRotationPoint;
        bool decNegRotation = sign == neg && _rotationIncrement > _prevRotationPoint;

        bool completePosRotation = sign == pos && _rotationIncrement >= _prevRotationPoint;
        bool completeNegRotation = sign == neg && _rotationIncrement <= _prevRotationPoint;

        if (decPosRotation || decNegRotation) // active rotation towards the previous point.
        {
            if (!InstantRotation)
            {
                transform.Rotate(_rotationAxis, _coolingSpeed * Time.deltaTime * sign);
                _rotationIncrement += _coolingSpeed * Time.deltaTime * sign;

                CheckForDestination();

                // corrects rotation to default. Needs to be this long because it won't get assigned quick enough in a local variable.
                if (HeatOnlyScale && (_prevRotationPoint == _firstHeatPoint) && ((sign == pos && _rotationIncrement > _firstHeatPoint - _smallIncrement) || (sign == neg && _rotationIncrement < _firstHeatPoint + _smallIncrement)))
                {
                    Invoke("ResetGauge", _smallDelay);
                }
                else if (!HeatOnlyScale && (_prevRotationPoint == _firstCoolPoint) && ((sign == pos && _rotationIncrement > _firstCoolPoint - _smallIncrement) || (sign == neg && _rotationIncrement < _firstCoolPoint + _smallIncrement)))
                {
                    Invoke("MinGauge", _smallDelay);
                }
            }
            else
            {
                JumpToPrevRotationPoint();

                // corrects rotation to default.
                if (_prevRotationPoint == _firstHeatPoint)
                {
                    Invoke("ResetGauge", _smallDelay);
                }
                else if (_prevRotationPoint == _firstCoolPoint)
                {
                    Invoke("MinGauge", _smallDelay);
                }
            }

            _didBackRot = true;
        }

        // If the cooling takes it back below a previous rotation point, that will then become the next point to reach.

        else if (completePosRotation || completeNegRotation)
        {
            JumpToPrevRotationPoint(); // corrects the imprecision.

            // Previous forward rotation check.
            if (_nextRotationPoint == _finalHeatPoint || !HeatOnlyScale)
            {
                MoveToPrevPoint = false;
                _didForwardRot = false;
            }
            else if (!_didForwardRot)
            {
                MoveToPrevPoint = false;
            }
            else
            {
                _didForwardRot = false;
            }

            if (RunTimeReposition)
            {
                UpdateRotationPoints(IncrementStatus.Back);
            }

            if (HeatOnlyScale)
            {
                _heatIndex -= (_heatIndex > _startHeatIndex) ? 1 : 0; // decreases _heatIndex unless start.
            }
            _coolIndex -= (_coolIndex > _startCoolIndex) ? 1 : 0; // decreases _coolIndex unless start.
            SetRotationPoints(_heatIndex, _coolIndex);

            CheckForDestination();

            // Repeated Decrement calls.

            if (HeatOnlyScale)
            {
                if (_fireCalls > 0)
                {
                    _fireCalls--;
                    MoveToPrevPoint = true;
                }
                else
                {
                    Invoke("SetFireCalls", _smallDelay);
                }
            }
            else
            {

                if (_iceCalls > 0)
                {
                    _iceCalls--;
                    MoveToPrevPoint = true;
                }
                else
                {
                    Invoke("SetIceCalls", _smallDelay);
                }
            }
        }
    }

    public void AwakenCoolDown()
    {
        // if rotation is disabled and isn't last point.
        if (!MoveToNextPoint)
        {
            AutoCoolOn = true;
        }
    }

    public void SetFireCalls()
    {
        const float off = 19.9f;
        if (_maxDegrees != off)
        {
            _fireCalls = (_firePercentage * FireCalls) - 1;
        }
        else
        {
            _fireCalls = FireCalls - 1;
        }
    }

    public void SetIceCalls()
    {
        const float off = 19.9f;
        if (_maxDegrees != off)
        {
            _iceCalls = (_icePercentage * IceCalls) - 1;
        }
        else
        {
            _iceCalls = IceCalls - 1;
        }
    }

    private int GetStartIndex()
    {
        for (int i = _firstHeatIndex; i < _heatRotationPoints.Length; i++)
        {
            if (_heatRotationPoints[i] == (_heatRotationPoints[_firstHeatIndex] + _minDegreesBelowStart))
            {
                return (++i); // Assign Next Index after Starting Point
            }
        }
        return 1;
    }

    public void ResetGauge()
    {
        if (RunTimeReposition)
        {
            UpdateRotationPoints(IncrementStatus.Reset);
        }

        // Resets rotation.
        transform.rotation = _defaultRotation;

        // Resets tracking-related variables
        _rotationIncrement = _initialHeatPoint;
        _heatIndex = _startHeatIndex;
        SetRotationPoints(_heatIndex, _coolIndex);
        MoveToNextPoint = false;
        MoveToPrevPoint = false;
        _didForwardRot = false;
        _didBackRot = false;
    }

    public void MinGauge()
    {
        if (RunTimeReposition)
        {
            UpdateRotationPoints(IncrementStatus.Max);
        }

        // Completes rotation.
        transform.rotation = _descendRotation;

        // Sets tracking-related variables to final destinations.
        _rotationIncrement = _firstCoolPoint;
        _coolIndex = _startCoolIndex;
        _heatIndex = _firstCoolPoint == _firstHeatPoint ? _secondHeatIndex : _firstHeatIndex;
        SetRotationPoints(_heatIndex, _coolIndex);
        MoveToNextPoint = false;
        MoveToPrevPoint = false;
        _didForwardRot = false;
        _didBackRot = false;
        AutoCoolOn = false;
    }

    public void MaxGauge()
    {
        if (RunTimeReposition)
        {
            UpdateRotationPoints(IncrementStatus.Max);
        }

        // Completes rotation.
        transform.rotation = _finalRotation;

        // Sets tracking-related variables to final destinations.
        _rotationIncrement = _finalHeatPoint;
        _heatIndex = _finalHeatIndex;
        SetRotationPoints(_heatIndex, _coolIndex);
        MoveToNextPoint = false;
        MoveToPrevPoint = false;
        _didForwardRot = false;
        _didBackRot = false;
        AutoCoolOn = false;
        if (AutoCoolTriggerOn)
        {
            Invoke("AwakenCoolDown", _autoCoolDelay);
        }
    }

    public enum IncrementStatus { Reset, Max, Forward, Back };

    private void UpdateRotationPoints(IncrementStatus status)
    {
        if (HeatOnlyScale)
        {
            SetEqualHeatPoints();
            SetEqualCoolPoints();

            // Indexes of Array
            _startHeatIndex = 1;
            _finalHeatIndex = _heatRotationPoints.Length - 1;

            // Elements of Array
            _firstHeatPoint = _heatRotationPoints[0];
            _finalHeatPoint = _heatRotationPoints[_finalHeatIndex];

            // if out of bounds, set to the last point. Minus one if it will be incremented afterwards.
            if (_heatIndex > _finalHeatIndex)
            {
                bool isEndPoint = (status == IncrementStatus.Reset || status == IncrementStatus.Max);
                _heatIndex = isEndPoint ? _finalHeatIndex : _finalHeatIndex - 1;
            }

            SetRotationPoints(_heatIndex, _coolIndex);

            // Rotation to default point adjusted depending on previous forwards or backwards motion.

            const float noXrotation = 0, noYrotation = 0;

            if (status == IncrementStatus.Forward)
            {
                _defaultRotation = transform.rotation * Quaternion.Euler(noXrotation, noYrotation, _nextRotationPoint - _firstHeatPoint);
            }
            else if (status == IncrementStatus.Back)
            {
                _defaultRotation = transform.rotation * Quaternion.Euler(noXrotation, noYrotation, _prevRotationPoint - _firstHeatPoint);
            }
            else if (status == IncrementStatus.Reset)
            {
                _defaultRotation = _finalRotation * Quaternion.Euler(noXrotation, noYrotation, _finalHeatPoint - _firstHeatPoint);
            }

            // Rotation to final point.
            _finalRotation = _defaultRotation * Quaternion.Euler(noXrotation, noYrotation, _firstHeatPoint - _finalHeatPoint);
        }
    }

    private void SetRotationPoints(int i, int j)
    {
        // Check for invalid indexes.
        int newFinalHeatIndex = _heatRotationPoints.Length - 1, newFinalCoolIndex = _coolRotationPoints.Length - 1;

        if (i > newFinalHeatIndex || i < _firstHeatIndex)
        {
            ResetGauge();
            return;
        }

        if (!HeatOnlyScale && (j > newFinalCoolIndex || j < _startCoolIndex))
        {
            MinGauge();
            return;
        }

        // Permutations to set next rotation point.
        if (HeatOnlyScale || _didForwardRot || _rotationIncrement == _firstCoolPoint || _rotationIncrement == _firstHeatPoint || _rotationIncrement == _finalHeatPoint)
        {
            _nextRotationPoint = _heatRotationPoints[i];
        }
        else if (_didBackRot)
        {
            _heatIndex = FindHeatIndex(j + 1);
            _nextRotationPoint = _heatRotationPoints[_heatIndex];
        }

        // Permutations to set previous rotation point.
        if (HeatOnlyScale)
        {
            _prevRotationPoint = _heatRotationPoints[i - 1];
        }
        else if (_rotationIncrement == _firstCoolPoint && _coolIndex != _startCoolIndex)
        {
            MinGauge();
            return;
        }
        else if (_rotationIncrement == _finalHeatPoint && _rotationIncrement != _firstCoolPoint)
        {
            _coolIndex = FindCoolIndex(i);
            _prevRotationPoint = _coolRotationPoints[_coolIndex];
        }
        else if (_didForwardRot || (_rotationIncrement == _firstHeatPoint && _rotationIncrement != _firstCoolPoint))
        {
            _coolIndex = FindCoolIndex(i - 1);
            _prevRotationPoint = _coolRotationPoints[_coolIndex];
        }
        else if (_didBackRot || _rotationIncrement == _firstCoolPoint)
        {
            _prevRotationPoint = _coolRotationPoints[j];
        }
    }


    // Function to find a heat rotation point nearest the previous cool point to rotate to.

    private int FindHeatIndex(int j)
    {
        float sign = Mathf.Sign(_nextRotationPoint - _prevRotationPoint);
        const float pos = 1.0f, neg = -1.0f;

        int nextIndex = _heatIndex;
        float minDifference = sign == pos ? _finalCoolPoint - _firstHeatPoint : _firstHeatPoint - _finalCoolPoint;

        for (int i = _startHeatIndex; i < _heatRotationPoints.Length; i++)
        {
            float heatRotation = _heatRotationPoints[i];
            float coolRotation = _coolRotationPoints[j];

            if (sign == pos && heatRotation > coolRotation || sign == neg && coolRotation > heatRotation)
            {
                float difference = sign == pos ? heatRotation - coolRotation : coolRotation - heatRotation;

                if (difference < minDifference)
                {
                    minDifference = difference;
                    nextIndex = i;
                }
            }
        }

        return nextIndex;
    }

    // Function to find a cool rotation point nearest the previous heat point to rotate to.

    private int FindCoolIndex(int i)
    {
        float sign = Mathf.Sign(_nextRotationPoint - _prevRotationPoint);
        const float pos = 1.0f, neg = -1.0f;

        int nextIndex = _coolIndex;
        float minDifference = sign == pos ? _finalHeatPoint - _firstCoolPoint : _firstCoolPoint - _finalHeatPoint;
        bool noResult = true;

        for (int j = _startCoolIndex; j < _coolRotationPoints.Length; j++)
        {
            float heatRotation = _heatRotationPoints[i];
            float coolRotation = _coolRotationPoints[j];

            if (sign == pos && heatRotation > coolRotation || sign == neg && coolRotation > heatRotation)
            {
                float difference = sign == pos ? heatRotation - coolRotation : coolRotation - heatRotation;

                if (difference < minDifference)
                {
                    minDifference = difference;
                    nextIndex = j;
                }
            }

            noResult = false;
        }

        if (noResult)
        {
            nextIndex--;
        }

        return nextIndex;
    }

    private void JumpToNextRotationPoint()
    {

        transform.Rotate(_rotationAxis, _nextRotationPoint - _rotationIncrement);
        transform.localRotation = RoundRotationPoint(transform.localRotation);
        _rotationIncrement = _nextRotationPoint;
    }

    private void JumpToPrevRotationPoint()
    {
        transform.Rotate(_rotationAxis, _prevRotationPoint - _rotationIncrement);
        transform.localRotation = RoundRotationPoint(transform.localRotation);
        _rotationIncrement = _prevRotationPoint;
    }

    private Quaternion RoundRotationPoint(Quaternion RotationPoint)
    {
        Vector3 eulerAngles = RotationPoint.eulerAngles;
        eulerAngles.x = RoundToOneDP(eulerAngles.x);
        eulerAngles.y = RoundToOneDP(eulerAngles.y);
        eulerAngles.z = RoundToOneDP(eulerAngles.z);

        return Quaternion.Euler(eulerAngles);
    }

    private float RoundToOneDP(float valueToRound)
    {
        return Mathf.Round(valueToRound * 10) / 10;
    }

    private void CheckForDestination()
    {
        const float origin = 0.0f;

        foreach (Destination destination in _destinations)
        {
            if (_rotationIncrement >= destination.minPosition && _rotationIncrement <= destination.maxPosition)
            {
                SetDisabled();
                OnComplete.Invoke(destination.name);
            }
        }
    }

    private void SetEqualHeatPoints()
    {
        const float off = 19.9f;
        const int oneHundredPercent = 100, oneStartPoint = 1, nothing = 0;

        // When the end points are equal and by percent.

        if (_maxDegrees != off)
        {
            _equalHeatPoints = oneHundredPercent;
            _equalHeatEndPoint = _maxDegrees;
        }

        // Sets equidistant heat scale rotation points.

        int numberOfPoints = _equalHeatPoints + oneStartPoint;

        if (_equalHeatPoints > nothing)
        {
            _heatRotationPoints = new float[numberOfPoints];
            for (int i = _startHeatIndex; i < numberOfPoints; i++)
            {
                _heatRotationPoints[i] = i * (_equalHeatEndPoint / _equalHeatPoints) - _minDegreesBelowStart;
            }
        }
    }

    private void SetEqualCoolPoints()
    {
        const float off = 19.9f;
        const int oneHundredPercent = 100, oneStartPoint = 1, nothing = 0;

        // When the end points are equal and by percent.

        if (_maxDegrees != off)
        {
            _equalCoolPoints = oneHundredPercent;
            _equalCoolEndPoint = _maxDegrees;
        }

        // Sets equidistant ice scale rotation points.

        int numberOfPoints = _equalCoolPoints + oneStartPoint;

        if (_equalCoolPoints > nothing)
        {
            _coolRotationPoints = new float[numberOfPoints];
            for (int i = _startCoolIndex; i < numberOfPoints; i++)
            {
                _coolRotationPoints[i] = i * (_equalCoolEndPoint / _equalCoolPoints) - _minDegreesBelowStart;
            }
        }
    }

    private void SetDisabled()
    {
        _fireCalls = 0;
        _iceCalls = 0;
        MoveToNextPoint = false;
        MoveToPrevPoint = false;
        AutoCoolOn = false;
        AutoCoolTriggerOn = false;
    }
}