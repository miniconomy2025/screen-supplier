using FluentAssertions;
using Moq;
using Reqnroll;
using ScreenProducerAPI.Services;
using ScreenProducerAPI.Services.BankServices;
using System.Threading.Tasks;

namespace ScreenProducerAPI.Test.StepDefinitions;

[Binding]
public sealed class BankIntegrationServiceStepDefinitions
{
    private Mock<IBankService> _mockBankService;
    private BankIntegrationService _bankIntegrationService;
    private bool _hasAccount;
    private bool _hasLoan;
    private bool _hasNotificationUrl;
    private bool _accountCreationWillFail;
    private bool _loanWillFail;
    private (bool accountCreated, bool loanTaken, bool notificationUrlSet) _result;

    public BankIntegrationServiceStepDefinitions()
    {
        _mockBankService = new Mock<IBankService>();
        _bankIntegrationService = new BankIntegrationService(_mockBankService.Object);
    }

    [Given(@"the bank integration service has no existing account")]
    public void GivenTheBankIntegrationServiceHasNoExistingAccount()
    {
        _hasAccount = false;
    }

    [Given(@"the bank integration service has an existing account")]
    public void GivenTheBankIntegrationServiceHasAnExistingAccount()
    {
        _hasAccount = true;
    }

    [Given(@"the bank integration service has no existing loan")]
    public void GivenTheBankIntegrationServiceHasNoExistingLoan()
    {
        _hasLoan = false;
    }

    [Given(@"the bank integration service has an existing loan")]
    public void GivenTheBankIntegrationServiceHasAnExistingLoan()
    {
        _hasLoan = true;
    }

    [Given(@"the bank integration service has no notification URL setup")]
    public void GivenTheBankIntegrationServiceHasNoNotificationUrlSetup()
    {
        _hasNotificationUrl = false;
    }

    [Given(@"the bank account creation will fail")]
    public void GivenTheBankAccountCreationWillFail()
    {
        _accountCreationWillFail = true;
        _mockBankService.Setup(x => x.TryInitializeBankAccountAsync())
            .ReturnsAsync(false);
    }

    [Given(@"the loan taking will fail")]
    public void GivenTheLoanTakingWillFail()
    {
        _loanWillFail = true;
        _mockBankService.Setup(x => x.TryTakeInitialLoanAsync())
            .ReturnsAsync(false);
    }

    [When(@"the bank integration service initializes")]
    public async Task WhenTheBankIntegrationServiceInitializes()
    {
        // Setup default successful behavior if not overridden by failure scenarios
        if (!_accountCreationWillFail)
        {
            _mockBankService.Setup(x => x.TryInitializeBankAccountAsync())
                .ReturnsAsync(true);
        }

        if (!_loanWillFail)
        {
            _mockBankService.Setup(x => x.TryTakeInitialLoanAsync())
                .ReturnsAsync(true);
        }

        _result = await _bankIntegrationService.InitializeAsync(_hasAccount, _hasLoan, _hasNotificationUrl);
    }

    [Then(@"the account should be created successfully")]
    public void ThenTheAccountShouldBeCreatedSuccessfully()
    {
        _result.accountCreated.Should().BeTrue();

        // Verify that TryInitializeBankAccountAsync was called only if there was no existing account
        if (!_hasAccount)
        {
            _mockBankService.Verify(x => x.TryInitializeBankAccountAsync(), Times.Once);
        }
        else
        {
            _mockBankService.Verify(x => x.TryInitializeBankAccountAsync(), Times.Never);
        }
    }

    [Then(@"the account should remain as existing")]
    public void ThenTheAccountShouldRemainAsExisting()
    {
        _result.accountCreated.Should().BeTrue();

        // Verify that TryInitializeBankAccountAsync was not called since account already exists
        _mockBankService.Verify(x => x.TryInitializeBankAccountAsync(), Times.Never);
    }

    [Then(@"the account creation should fail")]
    public void ThenTheAccountCreationShouldFail()
    {
        _result.accountCreated.Should().BeFalse();

        // Verify that TryInitializeBankAccountAsync was called and failed
        _mockBankService.Verify(x => x.TryInitializeBankAccountAsync(), Times.Once);
    }

    [Then(@"the loan should be taken successfully")]
    public void ThenTheLoanShouldBeTakenSuccessfully()
    {
        _result.loanTaken.Should().BeTrue();

        // Verify that TryTakeInitialLoanAsync was called only if there was no existing loan and account was created/existed
        if (!_hasLoan && _result.accountCreated)
        {
            _mockBankService.Verify(x => x.TryTakeInitialLoanAsync(), Times.Once);
        }
    }

    [Then(@"the loan should remain as existing")]
    public void ThenTheLoanShouldRemainAsExisting()
    {
        _result.loanTaken.Should().BeTrue();

        // Verify that TryTakeInitialLoanAsync was not called since loan already exists
        _mockBankService.Verify(x => x.TryTakeInitialLoanAsync(), Times.Never);
    }

    [Then(@"the loan should fail")]
    public void ThenTheLoanShouldFail()
    {
        _result.loanTaken.Should().BeFalse();

        // Verify that TryTakeInitialLoanAsync was called and failed
        _mockBankService.Verify(x => x.TryTakeInitialLoanAsync(), Times.Once);
    }

    [Then(@"no loan should be attempted")]
    public void ThenNoLoanShouldBeAttempted()
    {
        _result.loanTaken.Should().BeFalse();

        // Verify that TryTakeInitialLoanAsync was not called because account creation failed
        _mockBankService.Verify(x => x.TryTakeInitialLoanAsync(), Times.Never);
    }

    [Then(@"the notification URL should be set to true")]
    public void ThenTheNotificationUrlShouldBeSetToTrue()
    {
        _result.notificationUrlSet.Should().BeTrue();
    }

    [AfterScenario]
    public void AfterScenario()
    {
        // Reset mocks for next scenario
        _mockBankService.Reset();
        _hasAccount = false;
        _hasLoan = false;
        _hasNotificationUrl = false;
        _accountCreationWillFail = false;
        _loanWillFail = false;
    }
}