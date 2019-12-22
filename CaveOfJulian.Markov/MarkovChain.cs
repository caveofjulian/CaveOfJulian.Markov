﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using CaveOfJulian.Markov.Exceptions;
using CaveOfJulian.Markov.Extensions;
using MathNet.Numerics.LinearAlgebra;

namespace CaveOfJulian.Markov
{
    public class MarkovChain<T> : MarkovChain where T:Delegate
    {
        public T[,] Delegates { get; set; }

        public MarkovChain(Matrix<double> oneStepTransitionProbabilities, T[,] delegates, IStochastic numberGenerator = null) 
            : base(oneStepTransitionProbabilities,numberGenerator)
        {
            Delegates = delegates;
        }

        public MarkovChain(double[,] oneStepTransitionProbabilities, T[,] delegates, IStochastic numberGenerator = null) 
            : base(oneStepTransitionProbabilities,numberGenerator)
        {
            Delegates = delegates;
        }
        
        public void Run(int startState = 0)
        {
            object response = null;

            while (true)
            {
                var hasNextState = TryGetNextState(startState, out var nextState);
                if (!hasNextState) return;
                response = Delegates[startState, nextState].DynamicInvoke(response);
                startState = nextState;
            }
        }
    }

    public class MarkovChain
    {
        /// <summary>
        /// One step transition probabilities corresponding to the Markov Chain.
        /// </summary>
        public Matrix<double> OneStepTransitionProbabilities { get; set; }

        private readonly IStochastic _numberGenerator;

        public MarkovChain(Matrix<double> oneStepTransitionProbabilities, IStochastic numberGenerator = null)
        {
            OneStepTransitionProbabilities = oneStepTransitionProbabilities;
            _numberGenerator = numberGenerator ?? new Rnd();
        }

        public MarkovChain(double[,] oneStepTransitionProbabilities, IStochastic numberGenerator = null)
        {
            OneStepTransitionProbabilities = Matrix<double>.Build.DenseOfArray(oneStepTransitionProbabilities);
            _numberGenerator = numberGenerator ?? new Rnd();
        }

        /// <summary>
        /// Returns random end state, depending on the number of steps. The last chain is always returned, even if the chain ended prematurely.
        /// </summary>
        /// <param name="startState"></param>
        /// <returns></returns>        /// <param name="steps"></param>

        public int GetNextState(int startState, int steps)
        {
            for (var i = 0; i < steps; i++)
            {
                startState = GetNextState(startState);
            }

            return startState;
        }

        /// <summary>
        /// Returns the next state randomly based on the one step transition probabilities.
        /// Returns -1 when there is no feasible next state.
        /// </summary>
        /// <param name="startState">Starting state.</param>
        /// <returns></returns>
        public int GetNextState(int startState)
        {
            var randomProbability = _numberGenerator.NextDouble();

            var sum = 0d;

            for (var i = 0; i < OneStepTransitionProbabilities.ColumnCount; i++)
            {
                var probability = OneStepTransitionProbabilities[startState, i];
                sum += probability;
                if (randomProbability < sum) return i;
            }

            return -1;
        }

        /// <summary>
        /// Gets a state after N states have randomly been chosen based on the one step transition probabilities.
        /// A return value indicates whether there is a feasible next state.
        /// </summary>
        /// <param name="startState"></param>
        /// <param name="steps"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        public bool TryGetNextState(int startState, int steps, out int state)
        {
            state = -1;

            for (var i = 0; i < steps; i++)
            {
                var hasNextState = TryGetNextState(startState, out var next);
                if (!hasNextState) return false;
            }

            return true;
        }

        /// <summary>
        /// Gets a next state based on the one step transition probabilities.
        /// A return value indicates whether there is a feasible next state.
        /// </summary>
        /// <param name="startState"></param>
        /// <param name="next"></param>
        /// <returns></returns>
        public bool TryGetNextState(int startState, out int next)
        {
            next = -1;

            try
            {
                next = GetNextState(startState);
                return next != -1;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Returns the one step transition probability from the starting state to the next state.
        /// </summary>
        /// <param name="start">Starting state.</param>
        /// <param name="next">Next state.</param>
        /// <returns></returns>
        public double CalculateProbability(int start, int next) => OneStepTransitionProbabilities[start, next];

        /// <summary>
        /// 
        /// </summary>
        /// <param name="start">Starting state.</param>
        /// <param name="nextStates">Sequence of next states.</param>
        /// <returns></returns>
        public double CalculateProbability(int start, int[] nextStates) => CalculatePathProbability(start, nextStates);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="start">Starting state.</param>
        /// <param name="nextStates">Sequence of next states.</param>
        /// <returns></returns>
        public double CalculateProbability(int start, IList<int> nextStates) => CalculatePathProbability(start, nextStates);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="states">Sequence of states. The first index is the starting state.</param>
        /// <returns></returns>
        public double CalculateProbability(int[] states) => CalculatePathProbability(states);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="states">Sequence of states. The first index is the starting state.</param>
        /// <returns></returns>
        public double CalculateProbability(IList<int> states) => CalculatePathProbability(states);

        private double CalculatePathProbability(IEnumerable<int> steps)
        {
            double result = 1;

            if(steps is null) throw new ArgumentNullException(nameof(steps));

            var hasFirst = steps.GetEnumerator().MoveNext();

            if (!hasFirst) throw new InvalidMarkovOperationException($"{nameof(steps)} cannot be empty!");
            var currentState = steps.GetEnumerator().Current;

            while (steps.GetEnumerator().MoveNext())
            {
                var nextState = steps.GetEnumerator().Current;
                result *= OneStepTransitionProbabilities[currentState, nextState];
                currentState = nextState;
            }

            return result;
        }

        // This function is separated for performance reasons. 
        private double CalculatePathProbability(int start, IEnumerable<int> steps)
        {
            double result = 1;

            if (steps is null) throw new ArgumentNullException(nameof(steps));

            while (steps.GetEnumerator().MoveNext())
            {
                var nextState = steps.GetEnumerator().Current;
                result *= OneStepTransitionProbabilities[start, nextState];
                start = nextState;
            }

            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="state"></param>
        /// <param name="acceptedDifference"></param>
        /// <returns></returns>
        public bool IsAbsorbingState(int state, double acceptedDifference = 0.0000001)
        {
            return Math.Abs(OneStepTransitionProbabilities[state, state] - 1) < acceptedDifference;
        }

        public bool IsRecurrent(int state)
        {
            throw  new NotImplementedException();
        }

        public bool IsTransient(int state)
        {
            var cycleDetector = new CycleDetector(OneStepTransitionProbabilities);
            var cycles = cycleDetector.DetectCycles();

        }

        public double AverageSteps(int startState = 0)
        {
            var determinant = OneStepTransitionProbabilities.Determinant();

            if(determinant is 0) 
                throw new InvalidMarkovOperationException("Determinant cannot be 0!");

            throw new NotImplementedException();
        }

        /// <summary>
        /// Normalizes all row vectors to 1.0.
        /// </summary>
        public void Normalize()
        {
            if(OneStepTransitionProbabilities.ContainsNegativeValue()) 
                throw new NegativeProbabilityException("Matrix may not contain negative values!");

            OneStepTransitionProbabilities = OneStepTransitionProbabilities.NormalizeRows(1.0);

        }
    }
}

