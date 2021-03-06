﻿using System;
using System.Collections.Generic;
using System.Text;
using MathNet.Numerics.LinearAlgebra;

namespace CaveOfJulian.Markov
{
    /// <summary>
    /// This MarkovActionChain wraps a multidimensional array of Func<Tchain,TChain> to invoke the actions that have to be taken.
    /// </summary>
    /// <typeparam name="TChain"></typeparam>
    public class MarkovActionChain<TChain> : MarkovChain
    {
        public Func<TChain, TChain>[,] Actions { get; set; }

        public TChain LastValue { get; set; }

        public MarkovActionChain(Matrix<double> oneStepTransitionProbabilities, Func<TChain, TChain>[,] actions, TChain beginValue = default, IStochastic numberGenerator = null)
            : base(oneStepTransitionProbabilities, numberGenerator)
        {
            Actions = actions;

            if (!typeof(TChain).IsValueType)
            {
                LastValue = Activator.CreateInstance<TChain>();
            }
        }

        public MarkovActionChain(double[,] oneStepTransitionProbabilities, Func<TChain, TChain>[,] actions, TChain beginValue = default, IStochastic numberGenerator = null)
            : base(oneStepTransitionProbabilities, numberGenerator)
        {
            Actions = actions;

            if (!typeof(TChain).IsValueType)
            {
                LastValue = Activator.CreateInstance<TChain>();
            }
        }

        public void Run(int startState = 0, Predicate<TChain> quitCondition = null)
        {
            const int stateNotFound = -1;
            int nextState;

            while ((nextState = GetNextState(startState)) != stateNotFound)
            {
                if(quitCondition != null && quitCondition.Invoke(LastValue)) break;

                LastValue = Actions[startState, nextState].Invoke(LastValue); 
                startState = nextState;
            }
        }
    }

    public class MarkovActionChain : MarkovChain
    {
        public Action[,] Actions { get; set; }

        public MarkovActionChain(Matrix<double> oneStepTransitionProbabilities, Action[,] actions, IStochastic numberGenerator = null)
            : base(oneStepTransitionProbabilities, numberGenerator)
        {
            Actions = actions;
        }

        public MarkovActionChain(double[,] oneStepTransitionProbabilities, Action[,] actions, IStochastic numberGenerator = null)
            : base(oneStepTransitionProbabilities, numberGenerator)
        {
            Actions = actions;
        }

        public void Run(int startState = 0)
        {
            const int stateNotFound = -1;
            int nextState;

            while ((nextState = GetNextState(startState)) != stateNotFound)
            {
                Actions[startState, nextState].Invoke();
                startState = nextState;
            }
        }
    }
}
