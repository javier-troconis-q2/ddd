﻿using command.contracts;
using shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace command
{
    public static class StartIdentityVerificationCommand
    {
        public class VerifyIdentityResult
        {
            public VerifyIdentityResult(string transactionId, string result)
            {
                TransactionId = transactionId;
                Result = result;
            }

            public string TransactionId { get; }
            public string Result { get; }
        }

        public delegate Task<VerifyIdentityResult> VerifyIdentity(Ssn ssn);

        public struct StartIdentityVerificationCommandContext : IMessageHandler<IRecordedEvent<IApplicationStarted_V1>, StartIdentityVerificationCommandContext>
        {
            public readonly Ssn ApplicantSsn;

            public StartIdentityVerificationCommandContext(Ssn applicantSsn)
            {
                ApplicantSsn = applicantSsn;
            }

            public StartIdentityVerificationCommandContext Handle(IRecordedEvent<IApplicationStarted_V1> message)
            {
                return new StartIdentityVerificationCommandContext(message.Data.ApplicantSsn);
            }
        }

        public struct IdentityVerificationCompleted : IIdentityVerificationCompleted_V1
        {
            public IdentityVerificationCompleted(string transactionId, string result)
            {
                TransactionId = transactionId;
                Result = result;
            }

            public string TransactionId { get; }
            public string Result { get; }
        }

        public static async Task<IdentityVerificationCompleted> StartIdentityVerification(StartIdentityVerificationCommandContext context, VerifyIdentity verifyIdentity)
        {
            var result = await verifyIdentity(context.ApplicantSsn);
            return new IdentityVerificationCompleted(result.TransactionId, result.Result);
        }
    }
}
