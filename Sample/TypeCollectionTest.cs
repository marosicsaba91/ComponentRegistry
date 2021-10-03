/*
using System;
using System.Collections.Generic;
using ComponentDatabasesSystem;
using MUtility;
using UnityEngine;
using UnityEngine.Profiling; 

public class TypeCollectionTest : MonoBehaviour
{
    [SerializeField] int testCount = 1000;
    [SerializeField] SpeedTestButton speedTest;
    [SerializeField] CountTestButton countTest;
    
    IReadOnlyList<IPet> _pets;
    IReadOnlyList<Animal> _animals;
    IReadOnlyList<IPredator> _predators;
    IReadOnlyList<IPredator> _enabledPredators;
    
    IReadOnlyList<object> _predators2;
    IReadOnlyList<object> _pets2;
    
    void Awake()
    {
        Init();
    }
    
    void Init()
    {
        _pets = (IReadOnlyList<IPet>)ComponentDatabase.global.GetComponents<IPet>(enabledOnly: false);
        _animals = ComponentRegistry.GetAll<Animal>();
        _predators = ComponentRegistry.GetAll<IPredator>();
        _enabledPredators = ComponentRegistry.GetAllEnabled<IPredator>();
        _predators2 = ComponentRegistry.GetAll(typeof(IPredator));
        _pets2 = ComponentRegistry.GetAllEnabled(typeof(IPet));
    }

    void SpeedTest()
    {
        double allFindObjectsOfTypeTime = 0;
        double allComponentsTime = 0;
        double allFindObjectsOfTypeHeap= 0;
        double allComponentsHeap= 0;
        var allComponentsFoundCount = 0;
        var allFindObjectsOfTypeFoundCount = 0;
        for (var i = 0; i < testCount; i++)
        {

            long heap0 = Profiler.usedHeapSizeLong;
            DateTime time0 = DateTime.Now;
            
            _pets = ComponentRegistry.GetAll<IPet>();
            allComponentsFoundCount += _pets.Count;
            
            long heap1 = Profiler.usedHeapSizeLong;
            DateTime time1 = DateTime.Now;
            
            allComponentsTime += (time1 - time0).TotalMilliseconds;
            allComponentsHeap += (heap1 - heap0);

            
            heap0 = Profiler.usedHeapSizeLong;
            time0 = DateTime.Now;

            Animal[] animals = FindObjectsOfType<Animal>();
            allFindObjectsOfTypeFoundCount += animals.Length;

            heap1 = Profiler.usedHeapSizeLong;
            time1 = DateTime.Now;
            
            
            allFindObjectsOfTypeTime += (time1 - time0).TotalMilliseconds;
            allFindObjectsOfTypeHeap += (heap1 - heap0);
        }

        Debug.Log($" Component \t H: {allComponentsHeap}  T: {allComponentsTime}     N: {allComponentsFoundCount}");
        Debug.Log($" Unity  \t H: {allFindObjectsOfTypeHeap}  T: {allFindObjectsOfTypeTime}     N: {allFindObjectsOfTypeFoundCount}");
    }
 
    
    [Serializable] class SpeedTestButton : InspectorButton<TypeCollectionTest>
    {
        protected override void OnClick(TypeCollectionTest obj) => obj.SpeedTest();
    } 

    
    [Serializable] class CountTestButton : InspectorButton<TypeCollectionTest>
    {
        protected override void OnClick(TypeCollectionTest obj)
        { 
        }
    }
}
*/