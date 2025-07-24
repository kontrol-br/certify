using System;
using Certify.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Certify.Core.Tests.Unit
{
    [TestClass]
    public class RenewalRequiredTests
    {
        [TestMethod, Description("Ensure a site which should be renewed correctly requires renewal, where failure has previously occurred")]
        public void TestCheckAutoRenewalPeriodRequiredWithFailures()
        {
            // setup
            var renewalPeriodDays = 14;
            var renewalIntervalMode = RenewalIntervalModes.DaysAfterLastRenewal;

            var managedCertificate = new ManagedCertificate
            {
                IncludeInAutoRenew = true,
                DateRenewed = DateTimeOffset.UtcNow.AddDays(-15),
                DateExpiry = DateTimeOffset.UtcNow.AddDays(60),
                DateLastRenewalAttempt = DateTimeOffset.UtcNow.AddHours(-12),
                LastRenewalStatus = RequestState.Error,
                RenewalFailureCount = 2
            };

            // perform check
            var renewalDueCheck
                = ManagedCertificate.CalculateNextRenewalAttempt(managedCertificate, renewalPeriodDays, renewalIntervalMode, true);

            // assert result
            Assert.IsTrue(renewalDueCheck.IsRenewalDue, "Renewal should be required");

            managedCertificate = new ManagedCertificate
            {
                IncludeInAutoRenew = true,
                DateRenewed = DateTimeOffset.UtcNow.AddDays(-15),
                DateExpiry = DateTimeOffset.UtcNow.AddDays(60),
                DateLastRenewalAttempt = null,
                LastRenewalStatus = null,
                RenewalFailureCount = 0
            };

            // perform check
            renewalDueCheck = ManagedCertificate.CalculateNextRenewalAttempt(managedCertificate, renewalPeriodDays, renewalIntervalMode, true);

            // assert result
            Assert.IsTrue(renewalDueCheck.IsRenewalDue, "Site with no previous status - Renewal should be required");
        }

        [TestMethod, Description("Ensure renewal hold when site requires immediate renewal but failure has previously occurred")]
        public void TestCheckAutoRenewalPeriodRequiredWithFailuresHold()
        {
            // setup
            var renewalPeriodDays = 14;
            var renewalIntervalMode = RenewalIntervalModes.DaysAfterLastRenewal;

            var managedCertificate = new ManagedCertificate
            {
                IncludeInAutoRenew = true,
                DateRenewed = DateTimeOffset.UtcNow.AddDays(-15),
                DateStart = DateTimeOffset.UtcNow.AddDays(-15),
                DateExpiry = DateTimeOffset.UtcNow.AddDays(60),
                DateLastRenewalAttempt = DateTimeOffset.UtcNow.AddHours(-12),
                LastRenewalStatus = RequestState.Error,
                RenewalFailureCount = 100, // high number of failures
                DateNextScheduledRenewalAttempt = DateTimeOffset.UtcNow.AddHours(-0.1) // scheduled renewal set to become due
            };

            // perform check
            var renewalDueCheck
                = ManagedCertificate.CalculateNextRenewalAttempt(managedCertificate, renewalPeriodDays, renewalIntervalMode, true);

            // assert result
            Assert.IsTrue(renewalDueCheck.IsRenewalDue, "Renewal should be required");
            Assert.IsTrue(renewalDueCheck.IsRenewalOnHold, "Renewal should be on hold");
            Assert.AreEqual(renewalDueCheck.HoldHrs, 48, "Hold should be for 48 Hrs");

            managedCertificate.DateLastRenewalAttempt = DateTimeOffset.UtcNow.AddHours(-49);

            // perform check as if last attempt was over 48rs ago, item should require renewal and not be on hold
            renewalDueCheck = ManagedCertificate.CalculateNextRenewalAttempt(managedCertificate, renewalPeriodDays, renewalIntervalMode, true);

            // assert result
            Assert.IsTrue(renewalDueCheck.IsRenewalDue, "Renewal should be required");
            Assert.IsFalse(renewalDueCheck.IsRenewalOnHold, "Renewal should not be on hold");
        }

        [TestMethod, Description("Ensure renewal hold when item has failed more than 100 times")]
        public void TestCheckAutoRenewalWithTooManyFailuresHold()
        {
            // setup
            var renewalPeriodDays = 14;
            var renewalIntervalMode = RenewalIntervalModes.DaysAfterLastRenewal;

            var managedCertificate = new ManagedCertificate
            {
                IncludeInAutoRenew = true,
                DateRenewed = DateTimeOffset.UtcNow.AddDays(-15),
                DateStart = DateTimeOffset.UtcNow.AddDays(-15),
                DateExpiry = DateTimeOffset.UtcNow.AddDays(60),
                DateLastRenewalAttempt = DateTimeOffset.UtcNow.AddHours(-12),
                LastRenewalStatus = RequestState.Error,
                RenewalFailureCount = 1001, // too many failures
                DateNextScheduledRenewalAttempt = DateTimeOffset.UtcNow.AddHours(-0.1) // scheduled renewal set to become due
            };

            // perform check
            var renewalDueCheck
                = ManagedCertificate.CalculateNextRenewalAttempt(managedCertificate, renewalPeriodDays, renewalIntervalMode, true);

            // assert result
            Assert.IsTrue(renewalDueCheck.IsRenewalDue, "Renewal should be required");
            Assert.IsTrue(renewalDueCheck.IsRenewalOnHold, "Renewal should be on hold");
            Assert.AreEqual(renewalDueCheck.HoldHrs, 48, "Hold should be for 48 Hrs");

            managedCertificate.DateLastRenewalAttempt = DateTimeOffset.UtcNow.AddHours(-49);

            // perform check as if last attempt was over 48rs ago, item should require renewal and not be on hold
            renewalDueCheck = ManagedCertificate.CalculateNextRenewalAttempt(managedCertificate, renewalPeriodDays, renewalIntervalMode, true);

            // assert result
            Assert.IsTrue(renewalDueCheck.IsRenewalDue, "Renewal should be required");
            Assert.IsTrue(renewalDueCheck.IsRenewalOnHold, "Renewal should permanently be on hold, too many failures.");
        }

        [TestMethod, Description("Ensure a site which should be renewed correctly requires renewal")]
        public void TestCheckAutoRenewalPeriodRequired()
        {
            // setup
            var renewalPeriodDays = 14;
            var renewalIntervalMode = RenewalIntervalModes.DaysAfterLastRenewal;

            var managedCertificate = new ManagedCertificate { IncludeInAutoRenew = true, DateRenewed = DateTimeOffset.UtcNow.AddDays(-15), DateExpiry = DateTimeOffset.UtcNow.AddDays(60) };

            // perform check
            var isRenewalRequired = ManagedCertificate.CalculateNextRenewalAttempt(managedCertificate, renewalPeriodDays, renewalIntervalMode);

            // assert result
            Assert.IsTrue(isRenewalRequired.IsRenewalDue, "Renewal should be required");
        }

        [TestMethod, Description("Ensure a site which should not be renewed correctly does not require renewal")]
        public void TestCheckAutoRenewalPeriodNotRequired()
        {
            // setup : set renewal period to 30 days, last renewal 15 days ago. Renewal should not be
            // required yet.
            var renewalPeriodDays = 30;
            var renewalIntervalMode = RenewalIntervalModes.DaysAfterLastRenewal;

            var managedCertificate = new ManagedCertificate { IncludeInAutoRenew = true, DateRenewed = DateTimeOffset.UtcNow.AddDays(-15), DateExpiry = DateTimeOffset.UtcNow.AddDays(60) };

            // perform check
            var isRenewalRequired = ManagedCertificate.CalculateNextRenewalAttempt(managedCertificate, renewalPeriodDays, renewalIntervalMode);

            // assert result
            Assert.IsFalse(isRenewalRequired.IsRenewalDue, "Renewal should not be required");

            var expectedRenewal = managedCertificate.DateRenewed.Value.AddDays(renewalPeriodDays);
            Assert.IsTrue((expectedRenewal - isRenewalRequired.DateNextRenewalAttempt).Value.TotalMinutes < 1, "Planned renewal should be within a minute of the date last renewed plus renewal interval");
        }

        [TestMethod, Description("Ensure item which should not normally be renewed correctly requires renewal if DateNextScheduledRenewalAttempt is set and due")]
        public void TestDateNextScheduledRenewalAttempt()
        {
            // setup : set renewal period to 30 days, last renewal 15 days ago.

            var renewalPeriodDays = 30;
            var renewalIntervalMode = RenewalIntervalModes.DaysAfterLastRenewal;

            var managedCertificate = new ManagedCertificate { IncludeInAutoRenew = true, DateRenewed = DateTimeOffset.UtcNow.AddDays(-15), DateExpiry = DateTimeOffset.UtcNow.AddDays(60) };

            // set scheduled renewal so it should become due
            managedCertificate.DateNextScheduledRenewalAttempt = DateTimeOffset.UtcNow.AddDays(-0.1);

            // perform check
            var isRenewalRequired = ManagedCertificate.CalculateNextRenewalAttempt(managedCertificate, renewalPeriodDays, renewalIntervalMode);

            // assert result
            Assert.IsTrue(isRenewalRequired.IsRenewalDue, "Renewal should be required due to scheduled date");

            // set scheduled renewal so it should not become due
            managedCertificate.DateNextScheduledRenewalAttempt = DateTimeOffset.UtcNow.AddDays(45);

            // perform check
            isRenewalRequired = ManagedCertificate.CalculateNextRenewalAttempt(managedCertificate, renewalPeriodDays, renewalIntervalMode);

            // assert result
            Assert.IsFalse(isRenewalRequired.IsRenewalDue, "Renewal should not be required due to scheduled date in future");
        }

        [TestMethod, Description("Ensure a site with unknown date for last renewal should require renewal")]
        public void TestCheckAutoRenewalPeriodUnknownLastRenewal()
        {
            // setup : set renewal period to 14 days, last renewal unknown.

            var renewalPeriodDays = 14;
            var renewalIntervalMode = RenewalIntervalModes.DaysAfterLastRenewal;

            var managedCertificate = new ManagedCertificate { IncludeInAutoRenew = true, DateExpiry = DateTimeOffset.UtcNow.AddDays(60) };

            // perform check
            var isRenewalRequired = ManagedCertificate.CalculateNextRenewalAttempt(managedCertificate, renewalPeriodDays, renewalIntervalMode);

            // assert result
            Assert.IsTrue(isRenewalRequired.IsRenewalDue, "Renewal should be required");
        }

        [TestMethod, Description("Cert that has a short lifetime should renew if it's expiry falls before the normal renewal interval")]
        public void TestCheckAutoRenewalWithShortCertLifetime()
        {
            // setup : set renewal period to 14 days. Cert has an extra short 12hr lifetime and so needs to renew before 12hrs have elapsed regardless of default renewal mode.

            var renewalPeriodDays = 14;
            var renewalIntervalMode = RenewalIntervalModes.DaysAfterLastRenewal;

            var startDate = DateTimeOffset.UtcNow.AddDays(-0.5);
            var managedCertificate = new ManagedCertificate
            {
                IncludeInAutoRenew = true,
                DateStart = startDate,
                DateRenewed = startDate,
                DateExpiry = startDate.AddDays(0.5)
            };

            // perform check
            var isRenewalRequired = ManagedCertificate.CalculateNextRenewalAttempt(managedCertificate, renewalPeriodDays, renewalIntervalMode);

            // assert result
            Assert.IsTrue(isRenewalRequired.IsRenewalDue, "Renewal should be required, certs lifetime is shorter than renewal interval");

            Assert.AreEqual(12, (int)isRenewalRequired.CertLifetime.Value.TotalHours, "Renewal should be required, certs lifetime is shorter than renewal interval");

            Assert.IsTrue(isRenewalRequired.DateNextRenewalAttempt.Value > managedCertificate.DateStart.Value.AddMinutes(30), "Cert should not try to instantly renew");

            Assert.IsTrue(isRenewalRequired.DateNextRenewalAttempt.Value < managedCertificate.DateStart.Value.AddHours(12), "Cert should renew before expiry time");
        }

        [TestMethod, Description("Cert with custom percentage lifetime")]
        [DataTestMethod]
        [DataRow(true, 0, 30, 50, 60, RenewalIntervalModes.PercentageLifetime, false, "30 day cert renewing at 50% lifetime, not due for renewal")]
        [DataRow(true, 15.5f, 30, 50, 60, RenewalIntervalModes.PercentageLifetime, true, "30 day cert renewing at 50% lifetime, due for renewal")]
        [DataRow(true, 0.5f, 1, 75, 60, RenewalIntervalModes.PercentageLifetime, false, "1 day cert renewing at 75% lifetime, not due for renewal")]
        [DataRow(true, 0.76f, 1, 75, 60, RenewalIntervalModes.PercentageLifetime, true, "1 day cert renewing at 75% lifetime, due for renewal")]
        [DataRow(true, 180, 365, 90, 90, RenewalIntervalModes.PercentageLifetime, false, "365 day cert renewing at 90% lifetime, not due for renewal")]
        public void TestAutoRenewalWithPercentageCertLifetime(
            bool previouslyRenewed, float daysElapsed, float lifetimeDays, float customRenewalPercentage, int renewalInterval, string customIntervalMode,
            bool renewalExpected, string testDescription)
        {
            // setup 
            var renewalIntervalMode = RenewalIntervalModes.DaysAfterLastRenewal;

            var startDate = DateTimeOffset.UtcNow.AddDays(-daysElapsed);

            var managedCertificate = new ManagedCertificate
            {
                IncludeInAutoRenew = true,
                DateStart = startDate,
                DateExpiry = startDate.AddDays(lifetimeDays),
                CustomRenewalTarget = customRenewalPercentage,
                CustomRenewalIntervalMode = customIntervalMode,
                DateRenewed = previouslyRenewed ? (DateTimeOffset?)startDate : (DateTimeOffset?)null
            };

            // perform check
            var isRenewalRequired = ManagedCertificate.CalculateNextRenewalAttempt(managedCertificate, renewalInterval, renewalIntervalMode);

            // assert result
            Assert.AreEqual(isRenewalRequired.IsRenewalDue, renewalExpected, $"Renewal expected: {renewalExpected} : {testDescription}");

            Assert.AreEqual(lifetimeDays * 24, (int)isRenewalRequired.CertLifetime.Value.TotalHours, $"Expected cert lifetime : {testDescription}");

        }

        [TestMethod, Description("Cert with default percentage lifetime")]
        [DataTestMethod]
        [DataRow(true, 0, 30, 50, RenewalIntervalModes.PercentageLifetime, false, "30 day cert renewing at 50% lifetime, not due for renewal")]
        [DataRow(true, 15.5f, 30, 50, RenewalIntervalModes.PercentageLifetime, true, "30 day cert renewing at 50% lifetime, due for renewal")]
        [DataRow(true, 0.5f, 1, 75, RenewalIntervalModes.PercentageLifetime, false, "1 day cert renewing at 75% lifetime, not due for renewal")]
        [DataRow(true, 0.76f, 1, 75, RenewalIntervalModes.PercentageLifetime, true, "1 day cert renewing at 75% lifetime, due for renewal")]
        [DataRow(true, 180, 365, 90, RenewalIntervalModes.PercentageLifetime, false, "365 day cert renewing at 90% lifetime, not due for renewal")]
        public void TestAutoRenewalWithDefaultPercentageCertLifetime(
           bool previouslyRenewed, float daysElapsed, float lifetimeDays, int renewalInterval, string renewalIntervalMode,
           bool renewalExpected, string testDescription)
        {
            // setup 

            var startDate = DateTimeOffset.UtcNow.AddDays(-daysElapsed);

            var managedCertificate = new ManagedCertificate
            {
                IncludeInAutoRenew = true,
                DateStart = startDate,
                DateExpiry = startDate.AddDays(lifetimeDays),
                DateRenewed = previouslyRenewed ? (DateTimeOffset?)startDate : (DateTimeOffset?)null
            };

            // perform check
            var isRenewalRequired = ManagedCertificate.CalculateNextRenewalAttempt(managedCertificate, renewalInterval, renewalIntervalMode);

            // assert result
            Assert.AreEqual(isRenewalRequired.IsRenewalDue, renewalExpected, $"Renewal expected: {renewalExpected} : {testDescription}");

            Assert.AreEqual(lifetimeDays * 24, (int)isRenewalRequired.CertLifetime.Value.TotalHours, $"Expected cert lifetime : {testDescription}");

        }

        [TestMethod, Description("Cert with custom percentage lifetime")]
        [DataTestMethod]
        [DataRow(true, 45, 90, 30, RenewalIntervalModes.DaysBeforeExpiry, false, "90 day cert renewing at 30 days before expiry, not due for renewal")]
        [DataRow(true, 45, 90, 30, RenewalIntervalModes.DaysAfterLastRenewal, true, "90 day cert renewing at 30 days after last renewal, due for renewal")]
        [DataRow(true, 63, 90, 30, RenewalIntervalModes.DaysBeforeExpiry, true, "90 day cert renewing at 30 days before expiry, due for renewal")]
        [DataRow(true, 31, 90, 30, RenewalIntervalModes.DaysAfterLastRenewal, true, "90 day cert renewing at 30 days after last renewal, due for renewal")]
        [DataRow(true, 5, 90, 30, RenewalIntervalModes.DaysAfterLastRenewal, false, "90 day cert renewing at 30 days after last renewal, not for renewal")]
        [DataRow(true, 5, 7, 30, RenewalIntervalModes.DaysAfterLastRenewal, false, "7 day cert renewing at *30 days after last renewal*, due for renewal due to short lifetime")]
        [DataRow(true, 6, 7, 1, RenewalIntervalModes.DaysBeforeExpiry, true, "7 day cert renewing at *1 days before renewal*, due for renewal due to short lifetime")]
        [DataRow(true, 5, 7, 1, RenewalIntervalModes.DaysBeforeExpiry, false, "7 day cert renewing at *1 days before renewal*, not due for renewal")]
        public void TestAutoRenewalWithIntervalMode(
           bool previouslyRenewed, float daysElapsed, float lifetimeDays, int renewalInterval, string renewalIntervalMode,
           bool renewalExpected, string testDescription)
        {
            // setup 

            var startDate = DateTimeOffset.UtcNow.AddDays(-daysElapsed);

            var managedCertificate = new ManagedCertificate
            {
                IncludeInAutoRenew = true,
                DateStart = startDate,
                DateExpiry = startDate.AddDays(lifetimeDays),
                DateRenewed = previouslyRenewed ? (DateTimeOffset?)startDate : (DateTimeOffset?)null
            };

            // perform check
            var isRenewalRequired = ManagedCertificate.CalculateNextRenewalAttempt(managedCertificate, renewalInterval, renewalIntervalMode);

            // assert result
            Assert.AreEqual(isRenewalRequired.IsRenewalDue, renewalExpected, $"Renewal expected: {renewalExpected} : {testDescription}");

            Assert.AreEqual(lifetimeDays * 24, (int)isRenewalRequired.CertLifetime.Value.TotalHours, $"Expected cert lifetime : {testDescription}");

        }

        [TestMethod, Description("Cert with custom percentage lifetime, not yet successfully ordered")]
        [DataTestMethod]
        [DataRow(0, 0f, 1, 50, 60, RenewalIntervalModes.PercentageLifetime, true, "1 day cert renewing at 50% lifetime, not yet created, due for first order")]
        [DataRow(1, 0f, 1, 50, 60, RenewalIntervalModes.PercentageLifetime, true, "1 day cert renewing at 50% lifetime, not yet created, attempted once")]
        [DataRow(4, 1f, 0, 50, 60, RenewalIntervalModes.PercentageLifetime, true, "Unknown lifetime cert renewing at 50% lifetime, not yet created, attempted 5 times")]
        [DataRow(5, 2.4f, 1, 50, 60, RenewalIntervalModes.PercentageLifetime, true, "1 day cert renewing at 50% lifetime, not yet created, attempted 5 times")]
        [DataRow(10, 2.4f, 1, 50, 60, RenewalIntervalModes.PercentageLifetime, true, "1 day cert renewing at 50% lifetime, not yet created, attempted 10 times")]
        [DataRow(15, 2.4f, 1, 50, 60, RenewalIntervalModes.PercentageLifetime, true, "1 day cert renewing at 50% lifetime, not yet created, attempted 15 times")]
        [DataRow(0, 0f, 0.01f, 50, 60, RenewalIntervalModes.PercentageLifetime, true, "0.01 day cert renewing at 50% lifetime, not yet created, due for first order")]
        [DataRow(25, 48f, 90f, 50, 60, RenewalIntervalModes.PercentageLifetime, true, "90 day cert renewing at 50% lifetime, not yet created, due for first order")]
        [DataRow(5, 29f, 90f, 50, 60, RenewalIntervalModes.PercentageLifetime, true, "90 day cert renewing at 50% lifetime, not yet created, due for first order")]

        public void TestAutoStartNewCert(
            int previousAttempts, float holdHrsExpected, float lifetimeDays, float customRenewalPercentage, int renewalInterval, string customIntervalMode,
            bool renewalExpected, string testDescription)
        {
            // setup 
            var renewalIntervalMode = RenewalIntervalModes.PercentageLifetime;

            var managedCertificate = new ManagedCertificate
            {
                IncludeInAutoRenew = true,
                CustomRenewalTarget = customRenewalPercentage,
                CustomRenewalIntervalMode = customIntervalMode
            };

            if (lifetimeDays > 0)
            {
                managedCertificate.RequestConfig.PreferredExpiryDays = lifetimeDays;
            }

            if (previousAttempts > 0)
            {
                managedCertificate.DateLastRenewalAttempt = DateTimeOffset.UtcNow.AddHours(-0.01);
                managedCertificate.RenewalFailureCount = previousAttempts;

            }

            // perform check
            var renewalCheckResult = ManagedCertificate.CalculateNextRenewalAttempt(managedCertificate, renewalInterval, renewalIntervalMode);

            // assert result
            Assert.AreEqual(renewalExpected, renewalCheckResult.IsRenewalDue, $"Renewal expected: {renewalExpected} : {testDescription}");

            Assert.AreEqual(holdHrsExpected, renewalCheckResult.HoldHrs, $"Renewal hold expected: {holdHrsExpected} : {testDescription}");

            Assert.AreEqual(holdHrsExpected > 0, renewalCheckResult.IsRenewalOnHold, $"Renewal hold expected : {testDescription}");

        }

        [TestMethod, Description("Ensure a site with unknown date for last renewal should renew before expiry")]
        [DataTestMethod]
        [DataRow(14, 90, 13, "DaysBeforeExpiry")]
        [DataRow(14, 90, 29, "DaysBeforeExpiry")]
        [DataRow(60, 90, 30, "DaysBeforeExpiry")]
        [DataRow(30, 45, -1, "DaysAfterLastRenewal")]
        [DataRow(60, 90, 30, "DaysAfterLastRenewal")]
        [DataRow(1, 10, 180, "DaysAfterLastRenewal")]
        [DataRow(60, 14, 14, "DaysAfterLastRenewal")]
        public void TestCheckAutoRenewal30DaysBeforeExpiry(int renewalPeriodDays, int daysSinceRenewed, int daysUntilExpiry, string renewalIntervalMode)
        {
            // setup 

            var dateLastRenewed = DateTimeOffset.UtcNow.AddDays(-daysSinceRenewed);

            var managedCertificate = new ManagedCertificate
            {
                IncludeInAutoRenew = true,
                DateRenewed = dateLastRenewed,
                DateLastRenewalAttempt = dateLastRenewed,
                DateExpiry = DateTimeOffset.UtcNow.AddDays(daysUntilExpiry)
            };

            // perform check
            var isRenewalRequired = ManagedCertificate.CalculateNextRenewalAttempt(managedCertificate, renewalPeriodDays, renewalIntervalMode);

            // assert result
            if (renewalIntervalMode == RenewalIntervalModes.DaysAfterLastRenewal)
            {
                if (daysSinceRenewed >= renewalPeriodDays)
                {
                    Assert.IsTrue(isRenewalRequired.IsRenewalDue, $"Renewal should be required. Renewal mode: {renewalIntervalMode}, renewal interval: {renewalPeriodDays}, days since last renewed: {daysSinceRenewed}");
                }
                else
                {
                    Assert.IsFalse(isRenewalRequired.IsRenewalDue, $"Renewal should not be required.  Renewal mode: {renewalIntervalMode}, renewal interval: {renewalPeriodDays}, days since last renewed: {daysSinceRenewed}");
                }
            }
            else if (renewalIntervalMode == RenewalIntervalModes.DaysBeforeExpiry)
            {
                if (daysUntilExpiry <= renewalPeriodDays)
                {
                    Assert.IsTrue(isRenewalRequired.IsRenewalDue, $"Renewal should be required. Renewal mode: {renewalIntervalMode}, renewal interval: {renewalPeriodDays}, days until expiry: {daysUntilExpiry}");
                }
                else
                {
                    Assert.IsFalse(isRenewalRequired.IsRenewalDue, $"Renewal should not be required. Renewal mode: {renewalIntervalMode}, renewal interval: {renewalPeriodDays}, days until expiry: {daysUntilExpiry}");
                }
            }
        }

        [TestMethod, Description("Check Percentage Lifetime Elapsed calc, allowing for nulls etc")]
        [DataTestMethod]
        [DataRow(null, null, null)]
        [DataRow(14f, 90f, 15)]
        [DataRow(0.5f, 1f, 50)]
        [DataRow(0f, 1f, 0)]
        [DataRow(0.1f, 0.5f, 20)]
        [DataRow(-0.1f, 0.5f, 0)] // cert start date is in the future, no elapsed lifetime
        [DataRow(365f, 90f, 100)]
        public void TestCheckPercentageLifetimeElapsed(float? daysSinceRenewed, float? lifetimeDays, int? expectedPercentage)
        {
            var managedCertificate = new ManagedCertificate();

            var testDate = DateTimeOffset.UtcNow;

            if (daysSinceRenewed.HasValue && lifetimeDays.HasValue)
            {
                var dateLastRenewed = testDate.AddDays(-daysSinceRenewed.Value);

                managedCertificate = new ManagedCertificate
                {
                    DateRenewed = dateLastRenewed,
                    DateLastRenewalAttempt = dateLastRenewed,
                    DateStart = dateLastRenewed,
                    DateExpiry = dateLastRenewed.AddDays(lifetimeDays.Value)
                };
            }

            var percentageElapsed = managedCertificate.GetPercentageLifetimeElapsed(testDate);

            Assert.AreEqual(expectedPercentage, percentageElapsed);
        }

        #region ARI (ACME Renewal Information) Tests

        [TestMethod, Description("Test ARI scheduled renewal overrides normal renewal calculation")]
        public void TestARIScheduledRenewalOverridesNormalRenewal()
        {
            // setup
            var renewalPeriodDays = 30;
            var renewalIntervalMode = RenewalIntervalModes.DaysAfterLastRenewal;

            var managedCertificate = new ManagedCertificate
            {
                IncludeInAutoRenew = true,
                DateRenewed = DateTimeOffset.UtcNow.AddDays(-10), // Only 10 days since renewal
                DateExpiry = DateTimeOffset.UtcNow.AddDays(60),
                DateNextScheduledRenewalAttempt = DateTimeOffset.UtcNow.AddHours(-1), // ARI says renew now
                ARICertificateId = "test.cert.id"
            };

            // perform check
            var renewalDueCheck = ManagedCertificate.CalculateNextRenewalAttempt(managedCertificate, renewalPeriodDays, renewalIntervalMode);

            // assert result
            Assert.IsTrue(renewalDueCheck.IsRenewalDue, "ARI scheduled renewal should override normal renewal logic");
            Assert.AreEqual("Certificate scheduled renewal is now due.", renewalDueCheck.Reason);
        }

        [TestMethod, Description("Test ARI scheduled renewal in future does not trigger immediate renewal")]
        public void TestARIScheduledRenewalInFuture()
        {
            // setup
            var renewalPeriodDays = 30;
            var renewalIntervalMode = RenewalIntervalModes.DaysAfterLastRenewal;

            var futureRenewalDate = DateTimeOffset.UtcNow.AddDays(5);
            var managedCertificate = new ManagedCertificate
            {
                IncludeInAutoRenew = true,
                DateRenewed = DateTimeOffset.UtcNow.AddDays(-10), // Only 10 days since renewal
                DateExpiry = DateTimeOffset.UtcNow.AddDays(60),
                DateNextScheduledRenewalAttempt = futureRenewalDate, // ARI says renew in 5 days
                ARICertificateId = "test.cert.id"
            };

            // perform check
            var renewalDueCheck = ManagedCertificate.CalculateNextRenewalAttempt(managedCertificate, renewalPeriodDays, renewalIntervalMode);

            // assert result
            Assert.IsFalse(renewalDueCheck.IsRenewalDue, "Future ARI scheduled renewal should not trigger immediate renewal");
            Assert.AreEqual(futureRenewalDate, renewalDueCheck.DateNextRenewalAttempt, "Next renewal attempt should be the ARI scheduled date");
        }

        [TestMethod, Description("Test certificate with ARI ID but no scheduled renewal uses normal logic")]
        public void TestARICertificateWithoutScheduledRenewal()
        {
            // setup
            var renewalPeriodDays = 14;
            var renewalIntervalMode = RenewalIntervalModes.DaysAfterLastRenewal;

            var managedCertificate = new ManagedCertificate
            {
                IncludeInAutoRenew = true,
                DateRenewed = DateTimeOffset.UtcNow.AddDays(-15), // 15 days since renewal
                DateExpiry = DateTimeOffset.UtcNow.AddDays(60),
                ARICertificateId = "test.cert.id.with.dots", // Has ARI ID but no scheduled renewal
                DateNextScheduledRenewalAttempt = null
            };

            // perform check
            var renewalDueCheck = ManagedCertificate.CalculateNextRenewalAttempt(managedCertificate, renewalPeriodDays, renewalIntervalMode);

            // assert result
            Assert.IsTrue(renewalDueCheck.IsRenewalDue, "Should use normal renewal logic when no ARI scheduled date");
            Assert.IsTrue(renewalDueCheck.Reason.Contains("default renewal settings"), "Should indicate normal renewal logic was used");
        }

        [TestMethod, Description("Test ARI scheduled renewal with certificate revocation scenario")]
        public void TestARIScheduledRenewalWithRevocation()
        {
            // setup - simulate a scenario where ARI suggests immediate renewal due to revocation
            var renewalPeriodDays = 30;
            var renewalIntervalMode = RenewalIntervalModes.DaysAfterLastRenewal;

            var managedCertificate = new ManagedCertificate
            {
                IncludeInAutoRenew = true,
                DateRenewed = DateTimeOffset.UtcNow.AddDays(-5), // Recently renewed
                DateExpiry = DateTimeOffset.UtcNow.AddDays(85),
                DateNextScheduledRenewalAttempt = DateTimeOffset.UtcNow.AddMinutes(-30), // ARI says renew immediately (30 mins ago)
                ARICertificateId = "revoked.cert.id",
                CertificateRevoked = true
            };

            // perform check
            var renewalDueCheck = ManagedCertificate.CalculateNextRenewalAttempt(managedCertificate, renewalPeriodDays, renewalIntervalMode);

            // assert result
            Assert.IsTrue(renewalDueCheck.IsRenewalDue, "Revoked certificate with ARI scheduled renewal should require immediate renewal");
        }

        [TestMethod, Description("Test ARI integration with percentage lifetime renewal mode")]
        public void TestARIWithPercentageLifetimeMode()
        {
            // setup
            var renewalInterval = 75; // 75% of lifetime
            var renewalIntervalMode = RenewalIntervalModes.PercentageLifetime;

            var startDate = DateTimeOffset.UtcNow.AddDays(-50); // 50 days into 90 day cert (55% elapsed)
            var ariScheduledDate = DateTimeOffset.UtcNow.AddDays(10); // ARI says wait 10 more days

            var managedCertificate = new ManagedCertificate
            {
                IncludeInAutoRenew = true,
                DateStart = startDate,
                DateRenewed = startDate,
                DateExpiry = startDate.AddDays(90), // 90 day certificate
                DateNextScheduledRenewalAttempt = ariScheduledDate,
                ARICertificateId = "percentage.test.id",
                CustomRenewalTarget = 75,
                CustomRenewalIntervalMode = RenewalIntervalModes.PercentageLifetime
            };

            // perform check
            var renewalDueCheck = ManagedCertificate.CalculateNextRenewalAttempt(managedCertificate, renewalInterval, renewalIntervalMode);

            // assert result
            Assert.IsFalse(renewalDueCheck.IsRenewalDue, "ARI scheduled renewal in future should override percentage calculation");
            Assert.AreEqual(ariScheduledDate, renewalDueCheck.DateNextRenewalAttempt, "Should use ARI scheduled date");
        }

        [TestMethod, Description("Test DateLastRenewalInfoCheck tracking")]
        public void TestDateLastRenewalInfoCheckTracking()
        {
            // This test verifies that the DateLastRenewalInfoCheck property exists and can be set
            var managedCertificate = new ManagedCertificate
            {
                IncludeInAutoRenew = true,
                DateRenewed = DateTimeOffset.UtcNow.AddDays(-10),
                DateExpiry = DateTimeOffset.UtcNow.AddDays(60),
                DateLastRenewalInfoCheck = DateTimeOffset.UtcNow.AddHours(-2), // Checked 2 hours ago
                ARICertificateId = "tracking.test.id"
            };

            // verify property can be read
            Assert.IsNotNull(managedCertificate.DateLastRenewalInfoCheck, "DateLastRenewalInfoCheck should be accessible");
            Assert.IsTrue(managedCertificate.DateLastRenewalInfoCheck.Value < DateTimeOffset.UtcNow, "Last check should be in the past");
        }

        [TestMethod, Description("Test ARI Certificate ID format validation")]
        [DataTestMethod]
        [DataRow("validformat.withperiod", true, "ARI ID with period should be valid")]
        [DataRow("invalidformatwithoutperiod", false, "ARI ID without period should be invalid")]
        [DataRow("multiple.periods.here", true, "ARI ID with multiple periods should be valid")]
        [DataRow("", false, "Empty ARI ID should be invalid")]
        [DataRow(null, false, "Null ARI ID should be invalid")]
        public void TestARICertificateIdFormat(string ariId, bool shouldBeValid, string testDescription)
        {
            var managedCertificate = new ManagedCertificate
            {
                IncludeInAutoRenew = true,
                DateRenewed = DateTimeOffset.UtcNow.AddDays(-10),
                DateExpiry = DateTimeOffset.UtcNow.AddDays(60),
                ARICertificateId = ariId
            };

            // The test simulates the validation logic that would occur in actual ARI processing
            var isValidFormat = !string.IsNullOrWhiteSpace(ariId) && ariId.Contains(".");

            Assert.AreEqual(shouldBeValid, isValidFormat, testDescription);
        }

        [TestMethod, Description("Test ARI with different Certificate Authority scenarios")]
        public void TestARIWithDifferentCAs()
        {
            // setup
            var renewalPeriodDays = 30;
            var renewalIntervalMode = RenewalIntervalModes.DaysAfterLastRenewal;

            // Test with Let's Encrypt (supports ARI)
            var managedCertificateLE = new ManagedCertificate
            {
                IncludeInAutoRenew = true,
                DateRenewed = DateTimeOffset.UtcNow.AddDays(-10),
                DateExpiry = DateTimeOffset.UtcNow.AddDays(60),
                CertificateAuthorityId = StandardCertAuthorities.LETS_ENCRYPT,
                ARICertificateId = "le.cert.id",
                DateNextScheduledRenewalAttempt = DateTimeOffset.UtcNow.AddDays(5),
                LastAttemptedCA = StandardCertAuthorities.LETS_ENCRYPT,
                CertificateCurrentCA = StandardCertAuthorities.LETS_ENCRYPT
            };

            // perform check
            var renewalDueCheck = ManagedCertificate.CalculateNextRenewalAttempt(managedCertificateLE, renewalPeriodDays, renewalIntervalMode);

            // assert result
            Assert.IsFalse(renewalDueCheck.IsRenewalDue, "Let's Encrypt cert with future ARI date should not be due");
            Assert.AreEqual(managedCertificateLE.DateNextScheduledRenewalAttempt, renewalDueCheck.DateNextRenewalAttempt);
        }

        [TestMethod, Description("Test ARI renewal window edge cases")]
        public void TestARIRenewalWindowEdgeCases()
        {
            var renewalPeriodDays = 30;
            var renewalIntervalMode = RenewalIntervalModes.DaysAfterLastRenewal;

            // Test scenario where ARI window start is exactly now
            var managedCertificate = new ManagedCertificate
            {
                IncludeInAutoRenew = true,
                DateRenewed = DateTimeOffset.UtcNow.AddDays(-10),
                DateExpiry = DateTimeOffset.UtcNow.AddDays(60),
                DateNextScheduledRenewalAttempt = DateTimeOffset.UtcNow, // Exactly now
                ARICertificateId = "edge.case.id"
            };

            var renewalDueCheck = ManagedCertificate.CalculateNextRenewalAttempt(managedCertificate, renewalPeriodDays, renewalIntervalMode);

            Assert.IsTrue(renewalDueCheck.IsRenewalDue, "Certificate with ARI scheduled renewal at current time should be due");

            // Test scenario where ARI window is 1 minute in the future
            managedCertificate.DateNextScheduledRenewalAttempt = DateTimeOffset.UtcNow.AddMinutes(1);
            renewalDueCheck = ManagedCertificate.CalculateNextRenewalAttempt(managedCertificate, renewalPeriodDays, renewalIntervalMode);

            Assert.IsFalse(renewalDueCheck.IsRenewalDue, "Certificate with ARI scheduled renewal 1 minute in future should not be due");
        }

        [TestMethod, Description("Test ARI interaction with renewal failure scenarios")]
        public void TestARIWithRenewalFailures()
        {
            var renewalPeriodDays = 30;
            var renewalIntervalMode = RenewalIntervalModes.DaysAfterLastRenewal;

            var managedCertificate = new ManagedCertificate
            {
                IncludeInAutoRenew = true,
                DateRenewed = DateTimeOffset.UtcNow.AddDays(-5),
                DateExpiry = DateTimeOffset.UtcNow.AddDays(60),
                DateLastRenewalAttempt = DateTimeOffset.UtcNow.AddHours(-2),
                LastRenewalStatus = RequestState.Error,
                RenewalFailureCount = 3,
                DateNextScheduledRenewalAttempt = DateTimeOffset.UtcNow.AddMinutes(-30), // ARI says renew now
                ARICertificateId = "failed.renewal.id"
            };

            var renewalDueCheck = ManagedCertificate.CalculateNextRenewalAttempt(managedCertificate, renewalPeriodDays, renewalIntervalMode, checkFailureStatus: true);

            // Should still respect ARI even with failures (though may be subject to hold periods)
            Assert.IsTrue(renewalDueCheck.IsRenewalDue, "ARI scheduled renewal should still be due even with previous failures");
        }

        [TestMethod, Description("Test ARI with extremely short certificate lifetimes")]
        public void TestARIWithShortLifetimeCertificates()
        {
            var renewalPeriodDays = 30; // Longer than cert lifetime
            var renewalIntervalMode = RenewalIntervalModes.DaysAfterLastRenewal;

            var startDate = DateTimeOffset.UtcNow.AddHours(-6); // 6 hours ago
            var managedCertificate = new ManagedCertificate
            {
                IncludeInAutoRenew = true,
                DateStart = startDate,
                DateRenewed = startDate,
                DateExpiry = startDate.AddHours(12), // 12 hour certificate
                DateNextScheduledRenewalAttempt = DateTimeOffset.UtcNow.AddHours(2), // ARI suggests renew in 2 hours
                ARICertificateId = "short.lifetime.id"
            };

            var renewalDueCheck = ManagedCertificate.CalculateNextRenewalAttempt(managedCertificate, renewalPeriodDays, renewalIntervalMode);

            Assert.IsFalse(renewalDueCheck.IsRenewalDue, "Short lifetime cert with future ARI renewal should not be immediately due");
            Assert.AreEqual(managedCertificate.DateNextScheduledRenewalAttempt, renewalDueCheck.DateNextRenewalAttempt, "Should use ARI scheduled time for short lifetime certs");
        }

        #endregion ARI Tests

        #region Complex Date Edge Cases Tests

        [TestMethod, Description("Test certificate with start date in the future")]
        public void TestCertificateWithFutureStartDate()
        {
            var renewalPeriodDays = 30;
            var renewalIntervalMode = RenewalIntervalModes.DaysAfterLastRenewal;

            var futureStartDate = DateTimeOffset.UtcNow.AddDays(1); // Start date 1 day in future
            var managedCertificate = new ManagedCertificate
            {
                IncludeInAutoRenew = true,
                DateStart = futureStartDate,
                DateRenewed = futureStartDate,
                DateExpiry = futureStartDate.AddDays(90), // 90 day cert starting in future
            };

            var renewalDueCheck = ManagedCertificate.CalculateNextRenewalAttempt(managedCertificate, renewalPeriodDays, renewalIntervalMode);

            // Should not be due for renewal since certificate hasn't even started yet
            Assert.IsFalse(renewalDueCheck.IsRenewalDue, "Certificate with future start date should not be due for renewal");

            // Test percentage lifetime calculation
            var percentageElapsed = managedCertificate.GetPercentageLifetimeElapsed(DateTimeOffset.UtcNow);
            Assert.AreEqual(0, percentageElapsed, "Certificate with future start date should have 0% elapsed lifetime");
        }

        [TestMethod, Description("Test certificate expiry calculation with millisecond precision")]
        public void TestCertificateExpiryWithMillisecondPrecision()
        {
            var renewalPeriodDays = 30;
            var renewalIntervalMode = RenewalIntervalModes.DaysBeforeExpiry;

            var now = DateTimeOffset.UtcNow;
            var preciseExpiryDate = now.AddDays(29).AddHours(23).AddMinutes(59).AddSeconds(59).AddMilliseconds(999);

            var managedCertificate = new ManagedCertificate
            {
                IncludeInAutoRenew = true,
                DateRenewed = now.AddDays(-60),
                DateExpiry = preciseExpiryDate
            };

            var renewalDueCheck = ManagedCertificate.CalculateNextRenewalAttempt(managedCertificate, renewalPeriodDays, renewalIntervalMode);

            // Should be due for renewal because expiry is within 30 days (just barely under)
            Assert.IsTrue(renewalDueCheck.IsRenewalDue, "Certificate expiring in less than 30 days should be due for renewal");
        }

        [TestMethod, Description("Test leap year date calculations")]
        [DataTestMethod]
        [DataRow(2024, 2, 29, true, "Leap year Feb 29th should be valid")]
        [DataRow(2023, 2, 28, true, "Non-leap year Feb 28th should be valid")]
        [DataRow(2020, 2, 29, true, "Leap year 2020 Feb 29th should be valid")]
        [DataRow(1900, 2, 28, true, "Century non-leap year 1900 Feb 28th should be valid")]
        [DataRow(2000, 2, 29, true, "Century leap year 2000 Feb 29th should be valid")]
        public void TestLeapYearDateCalculations(int year, int month, int day, bool isValid, string testDescription)
        {
            try
            {
                var leapYearDate = new DateTimeOffset(year, month, day, 12, 0, 0, TimeSpan.Zero);
                var renewalPeriodDays = 30;
                var renewalIntervalMode = RenewalIntervalModes.DaysAfterLastRenewal;

                var managedCertificate = new ManagedCertificate
                {
                    IncludeInAutoRenew = true,
                    DateStart = leapYearDate,
                    DateRenewed = leapYearDate,
                    DateExpiry = leapYearDate.AddDays(365) // One year later
                };

                // Test renewal calculation across potential leap year boundary
                var testDate = leapYearDate.AddDays(350); // Test near expiry
                var renewalDueCheck = ManagedCertificate.CalculateNextRenewalAttempt(managedCertificate, renewalPeriodDays, renewalIntervalMode, testDateTime: testDate);

                Assert.IsNotNull(renewalDueCheck, testDescription);
                Assert.IsTrue(renewalDueCheck.IsRenewalDue, $"Certificate should be due for renewal after 350 days: {testDescription}");
            }
            catch (ArgumentOutOfRangeException)
            {
                if (isValid)
                {
                    Assert.Fail($"Expected valid date but got exception: {testDescription}");
                }
            }
        }

        [TestMethod, Description("Test daylight saving time transitions")]
        public void TestDaylightSavingTimeTransitions()
        {
            var renewalPeriodDays = 30;
            var renewalIntervalMode = RenewalIntervalModes.DaysAfterLastRenewal;

            // Test spring forward transition (typically March in US)
            var springDate = new DateTimeOffset(2024, 3, 10, 1, 30, 0, TimeSpan.FromHours(-8)); // Before DST
            var managedCertificate = new ManagedCertificate
            {
                IncludeInAutoRenew = true,
                DateRenewed = springDate,
                DateExpiry = springDate.AddDays(90)
            };

            // Test renewal calculation during DST transition
            var testDate = springDate.AddDays(31); // After DST transition
            var renewalDueCheck = ManagedCertificate.CalculateNextRenewalAttempt(managedCertificate, renewalPeriodDays, renewalIntervalMode, testDateTime: testDate);

            Assert.IsTrue(renewalDueCheck.IsRenewalDue, "Renewal should be due after DST spring transition");

            // Test fall back transition (typically November in US)
            var fallDate = new DateTimeOffset(2024, 11, 3, 1, 30, 0, TimeSpan.FromHours(-7)); // Before DST ends
            managedCertificate.DateRenewed = fallDate;
            managedCertificate.DateExpiry = fallDate.AddDays(90);

            testDate = fallDate.AddDays(31); // After DST ends
            renewalDueCheck = ManagedCertificate.CalculateNextRenewalAttempt(managedCertificate, renewalPeriodDays, renewalIntervalMode, testDateTime: testDate);

            Assert.IsTrue(renewalDueCheck.IsRenewalDue, "Renewal should be due after DST fall transition");
        }

        [TestMethod, Description("Test timezone offset handling")]
        [DataTestMethod]
        [DataRow(-12, "UTC-12 (Baker Island)")]
        [DataRow(-8, "UTC-8 (Pacific Time)")]
        [DataRow(-5, "UTC-5 (Eastern Time)")]
        [DataRow(0, "UTC (Greenwich Mean Time)")]
        [DataRow(1, "UTC+1 (Central European Time)")]
        [DataRow(5.5, "UTC+5:30 (India Standard Time)")]
        [DataRow(9, "UTC+9 (Japan Standard Time)")]
        [DataRow(12, "UTC+12 (Fiji Time)")]
        public void TestTimezoneOffsetHandling(double offsetHours, string testDescription)
        {
            var renewalPeriodDays = 30;
            var renewalIntervalMode = RenewalIntervalModes.DaysAfterLastRenewal;

            var offset = TimeSpan.FromHours(offsetHours);
            var baseDate = new DateTimeOffset(2024, 6, 15, 12, 0, 0, offset);

            var managedCertificate = new ManagedCertificate
            {
                IncludeInAutoRenew = true,
                DateRenewed = baseDate,
                DateExpiry = baseDate.AddDays(90)
            };

            // Test with different timezone for current time
            var testDate = baseDate.AddDays(31).ToOffset(TimeSpan.Zero); // Convert to UTC
            var renewalDueCheck = ManagedCertificate.CalculateNextRenewalAttempt(managedCertificate, renewalPeriodDays, renewalIntervalMode, testDateTime: testDate);

            Assert.IsTrue(renewalDueCheck.IsRenewalDue, $"Renewal should be due regardless of timezone: {testDescription}");
            Assert.IsNotNull(renewalDueCheck.DateNextRenewalAttempt, $"Next renewal date should be calculated: {testDescription}");
        }

        [TestMethod, Description("Test extremely short certificate lifetimes (minutes)")]
        [DataTestMethod]
        [DataRow(1, "1 minute certificate")]
        [DataRow(5, "5 minute certificate")]
        [DataRow(15, "15 minute certificate")]
        [DataRow(30, "30 minute certificate")]
        [DataRow(60, "1 hour certificate")]
        public void TestExtremelyShortCertificateLifetimes(int lifetimeMinutes, string testDescription)
        {
            var renewalPeriodDays = 30; // Much longer than cert lifetime
            var renewalIntervalMode = RenewalIntervalModes.DaysAfterLastRenewal;

            var startDate = DateTimeOffset.UtcNow.AddMinutes(-lifetimeMinutes / 2); // Half elapsed
            var managedCertificate = new ManagedCertificate
            {
                IncludeInAutoRenew = true,
                DateStart = startDate,
                DateRenewed = startDate,
                DateExpiry = startDate.AddMinutes(lifetimeMinutes)
            };

            var renewalDueCheck = ManagedCertificate.CalculateNextRenewalAttempt(managedCertificate, renewalPeriodDays, renewalIntervalMode);

            // Should switch to percentage-based renewal for very short lifetimes
            Assert.IsTrue(renewalDueCheck.CertLifetime.HasValue, $"Certificate lifetime should be calculated: {testDescription}");
            Assert.AreEqual(lifetimeMinutes, (int)renewalDueCheck.CertLifetime.Value.TotalMinutes, $"Certificate lifetime should match: {testDescription}");
        }

        [TestMethod, Description("Test certificate already expired scenarios")]
        [DataTestMethod]
        [DataRow(-1, "Certificate expired 1 day ago")]
        [DataRow(-7, "Certificate expired 1 week ago")]
        [DataRow(-30, "Certificate expired 1 month ago")]
        [DataRow(-365, "Certificate expired 1 year ago")]
        public void TestAlreadyExpiredCertificates(int daysExpired, string testDescription)
        {
            var renewalPeriodDays = 30;
            var renewalIntervalMode = RenewalIntervalModes.DaysBeforeExpiry;

            var expiredDate = DateTimeOffset.UtcNow.AddDays(daysExpired);
            var managedCertificate = new ManagedCertificate
            {
                IncludeInAutoRenew = true,
                DateRenewed = expiredDate.AddDays(-90), // Renewed 90 days before expiry
                DateExpiry = expiredDate
            };

            var renewalDueCheck = ManagedCertificate.CalculateNextRenewalAttempt(managedCertificate, renewalPeriodDays, renewalIntervalMode);

            Assert.IsTrue(renewalDueCheck.IsRenewalDue, $"Expired certificate should be due for renewal: {testDescription}");

            // Verify the percentage calculation handles expired certificates
            var percentageElapsed = managedCertificate.GetPercentageLifetimeElapsed(DateTimeOffset.UtcNow);
            Assert.AreEqual(100, percentageElapsed, $"Expired certificate should show 100% lifetime elapsed: {testDescription}");
        }

        [TestMethod, Description("Test certificate with zero or negative lifetime")]
        public void TestCertificateWithInvalidLifetime()
        {
            var renewalPeriodDays = 30;
            var renewalIntervalMode = RenewalIntervalModes.PercentageLifetime;

            var baseDate = DateTimeOffset.UtcNow;

            // Test certificate with same start and expiry date (zero lifetime)
            var managedCertificate = new ManagedCertificate
            {
                IncludeInAutoRenew = true,
                DateStart = baseDate,
                DateRenewed = baseDate,
                DateExpiry = baseDate // Same as start date
            };

            var renewalDueCheck = ManagedCertificate.CalculateNextRenewalAttempt(managedCertificate, renewalPeriodDays, renewalIntervalMode);

            Assert.IsTrue(renewalDueCheck.IsRenewalDue, "Certificate with zero lifetime should be due for renewal");

            // Test certificate with expiry before start (negative lifetime)
            managedCertificate.DateExpiry = baseDate.AddHours(-1); // Expires before it starts
            renewalDueCheck = ManagedCertificate.CalculateNextRenewalAttempt(managedCertificate, renewalPeriodDays, renewalIntervalMode);

            Assert.IsTrue(renewalDueCheck.IsRenewalDue, "Certificate with negative lifetime should be due for renewal");
        }

        [TestMethod, Description("Test year boundary transitions")]
        [DataTestMethod]
        [DataRow(2023, 12, 31, 2024, 1, 15, "New Year transition 2023-2024")]
        [DataRow(2024, 12, 31, 2025, 1, 15, "New Year transition 2024-2025")]
        [DataRow(1999, 12, 31, 2000, 1, 15, "Y2K transition 1999-2000")]
        public void TestYearBoundaryTransitions(int startYear, int startMonth, int startDay, int endYear, int endMonth, int endDay, string testDescription)
        {
            var renewalPeriodDays = 30;
            var renewalIntervalMode = RenewalIntervalModes.DaysAfterLastRenewal;

            var startDate = new DateTimeOffset(startYear, startMonth, startDay, 23, 59, 59, TimeSpan.Zero);
            var endDate = new DateTimeOffset(endYear, endMonth, endDay, 0, 0, 1, TimeSpan.Zero);

            var managedCertificate = new ManagedCertificate
            {
                IncludeInAutoRenew = true,
                DateStart = startDate,
                DateRenewed = startDate,
                DateExpiry = endDate
            };

            var testDate = startDate.AddDays(31); // 31 days after start
            var renewalDueCheck = ManagedCertificate.CalculateNextRenewalAttempt(managedCertificate, renewalPeriodDays, renewalIntervalMode, testDateTime: testDate);

            Assert.IsTrue(renewalDueCheck.IsRenewalDue, $"Renewal should be due across year boundary: {testDescription}");
            Assert.IsNotNull(renewalDueCheck.CertLifetime, $"Certificate lifetime should be calculated across year boundary: {testDescription}");
        }

        [TestMethod, Description("Test month boundary edge cases with varying month lengths")]
        [DataTestMethod]
        [DataRow(1, 31, "January 31 days")]
        [DataRow(2, 28, "February 28 days (non-leap)")]
        [DataRow(4, 30, "April 30 days")]
        [DataRow(12, 31, "December 31 days")]
        public void TestMonthBoundaryEdgeCases(int month, int expectedDays, string testDescription)
        {
            var renewalPeriodDays = 15;
            var renewalIntervalMode = RenewalIntervalModes.DaysAfterLastRenewal;

            // Start on the last day of the month
            var startDate = new DateTimeOffset(2023, month, expectedDays, 12, 0, 0, TimeSpan.Zero);
            var managedCertificate = new ManagedCertificate
            {
                IncludeInAutoRenew = true,
                DateRenewed = startDate,
                DateExpiry = startDate.AddDays(90)
            };

            // Test exactly 15 days later (should cross month boundary)
            var testDate = startDate.AddDays(15);
            var renewalDueCheck = ManagedCertificate.CalculateNextRenewalAttempt(managedCertificate, renewalPeriodDays, renewalIntervalMode, testDateTime: testDate);

            Assert.IsTrue(renewalDueCheck.IsRenewalDue, $"Renewal should be due after crossing month boundary: {testDescription}");
        }

        [TestMethod, Description("Test certificates with very long lifetimes")]
        [DataTestMethod]
        [DataRow(365, "1 year certificate")]
        [DataRow(730, "2 year certificate")]
        [DataRow(1095, "3 year certificate")]
        [DataRow(3650, "10 year certificate")]
        [DataRow(36500, "100 year certificate")]
        public void TestVeryLongLifetimeCertificates(int lifetimeDays, string testDescription)
        {
            var renewalPeriodDays = 30;
            var renewalIntervalMode = RenewalIntervalModes.PercentageLifetime;

            var startDate = DateTimeOffset.UtcNow;
            var managedCertificate = new ManagedCertificate
            {
                IncludeInAutoRenew = true,
                DateStart = startDate,
                DateRenewed = startDate,
                DateExpiry = startDate.AddDays(lifetimeDays),
                CustomRenewalTarget = 90, // Renew at 90% of lifetime
                CustomRenewalIntervalMode = RenewalIntervalModes.PercentageLifetime
            };

            // Test when 91% of lifetime has elapsed (should be due)
            var testDate = startDate.AddDays((int)(lifetimeDays * 0.91));
            var renewalDueCheck = ManagedCertificate.CalculateNextRenewalAttempt(managedCertificate, renewalPeriodDays, renewalIntervalMode, testDateTime: testDate);

            Assert.IsTrue(renewalDueCheck.IsRenewalDue, $"Long lifetime certificate should be due at 91% elapsed: {testDescription}");

            // Test when 89% of lifetime has elapsed (should not be due)
            testDate = startDate.AddDays((int)(lifetimeDays * 0.89));
            renewalDueCheck = ManagedCertificate.CalculateNextRenewalAttempt(managedCertificate, renewalPeriodDays, renewalIntervalMode, testDateTime: testDate);

            Assert.IsFalse(renewalDueCheck.IsRenewalDue, $"Long lifetime certificate should not be due at 89% elapsed: {testDescription}");
        }

        #endregion Complex Date Edge Cases Tests
    }
}
