﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CSharpPaxosRuntime.Messaging;
using CSharpPaxosRuntime.Messaging.Bus;
using CSharpPaxosRuntime.Models;
using CSharpPaxosRuntime.Models.PaxosSpecificMessageTypes;
using CSharpPaxosRuntime.Roles.RolesGeneric;

namespace CSharpPaxosRuntime.Roles.Leader.LeaderStrategies
{
    public class ReceiveUpdatedBallotNumberStrategy : IMessageStrategy
    {
        public event EventHandler OnBallotAdopted;
        public event EventHandler OnBallotRejected;

        public void Execute(MessageStrategyExecuteArg<IMessage> obj)
        {
            if (!(obj.Message is SolicitateBallotResponse))
            {
                throw new MessageStrategyException("This strategy shouldn't be invoked with this message type");
            }

            SolicitateBallotResponse response = obj.Message as SolicitateBallotResponse;
            LeaderState state = obj.RoleState as LeaderState;

            if (ballotIsApproved(state.BallotNumber, response.BallotNumber))
            {
                onBallotApproved(state, response);
            }
            else if (higherBallotHasBeenApproved(state.BallotNumber, response.BallotNumber))
            {
                onBallotRejected(state, response);
            }
        }

        private bool higherBallotHasBeenApproved(BallotNumber stateBallotNumber, BallotNumber responseBallotNumber)
        {
            return responseBallotNumber > stateBallotNumber;
        }

        private void onBallotApproved(LeaderState state, SolicitateBallotResponse response)
        {
            removeAcceptorFromWaitingQueue(state, response.MessageSender);
            storePreviousAcceptedValuesFromAcceptors(state, response.Decision);
            if (majorityOfAcceptorsReplied(state))
            {
                OnBallotAdopted?.Invoke(this, EventArgs.Empty);
            }
        }

        private bool majorityOfAcceptorsReplied(LeaderState state)
        {
            return state.WaitingFor.Count < state.Acceptors.Count / 2;
        }

        private void storePreviousAcceptedValuesFromAcceptors(LeaderState state, VoteDecision responseDecision)
        {
            state.ValuesAcceptedByAcceptors.Add(responseDecision);
        }

        private void removeAcceptorFromWaitingQueue(LeaderState state, MessageSender acceptor)
        {
            state.WaitingFor.Remove(acceptor);
        }

        private void onBallotRejected(LeaderState state, SolicitateBallotResponse response)
        {
            updateBallotNumber(state, response);
            clearState(state);
            OnBallotRejected?.Invoke(this, EventArgs.Empty);
        }

        private void clearState(LeaderState state)
        {
            state.WaitingFor.Clear();
            state.ValuesAcceptedByAcceptors.Clear();
        }

        private void updateBallotNumber(LeaderState state, SolicitateBallotResponse response)
        {
            state.BallotNumber = response.BallotNumber;
        }

        private bool ballotIsApproved(BallotNumber stateBallotNumber, BallotNumber responseBallotNumber)
        {
            return stateBallotNumber == responseBallotNumber;
        }
    }
}