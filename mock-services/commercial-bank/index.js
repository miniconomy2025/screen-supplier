const express = require('express');
const app = express();
const port = 3000;

app.use(express.json());

// In-memory storage
let accounts = {};
let transactions = {};
let loans = {};
let notificationUrls = {};

let accountCounter = 1000;
let transactionCounter = 1;
let loanCounter = 1;

// Utility functions
function generateAccountNumber() {
    return `ACC${String(accountCounter++).padStart(6, '0')}`;
}

function generateTransactionNumber() {
    return `TXN${String(transactionCounter++).padStart(8, '0')}`;
}

function generateLoanNumber() {
    return `LOAN${String(loanCounter++).padStart(6, '0')}`;
}

function getCurrentUnixTimestamp() {
    return Math.floor(Date.now() / 1000);
}

// Simple middleware to track which account is making the request
// For simplicity, we'll use a single default account after creation
let defaultAccount = null;

function getAccountNumber(req, res, next) {
    // If no default account exists, return error for authenticated endpoints
    if (!defaultAccount && req.path !== '/account') {
        return res.status(401).json({ error: 'No account created yet' });
    }
    
    req.accountNumber = defaultAccount;
    next();
}

// Routes
// Create Account
app.post('/account', (req, res) => {
    const accountNumber = generateAccountNumber();
    
    accounts[accountNumber] = {
        accountNumber: accountNumber,
        balance: 0,
        frozen: false,
        createdAt: getCurrentUnixTimestamp()
    };

    // Set as default account for simplicity
    defaultAccount = accountNumber;

    console.log(`Created account: ${accountNumber} (set as default)`);
    
    res.json({
        account_number: accountNumber
    });
});

// Get Account Number
app.get('/account/me', getAccountNumber, (req, res) => {
    const accountNumber = req.accountNumber;
    
    if (!accounts[accountNumber]) {
        return res.status(404).json({ error: 'Account not found' });
    }

    res.json({
        account_number: accountNumber
    });
});

// Set Notification URL
app.post('/account/me/notify', getAccountNumber, (req, res) => {
    const accountNumber = req.accountNumber;
    const { notification_url } = req.body;

    if (!notification_url) {
        return res.status(400).json({ error: 'notification_url is required' });
    }

    notificationUrls[accountNumber] = notification_url;
    console.log(`Set notification URL for ${accountNumber}: ${notification_url}`);

    res.json({
        success: true
    });
});

// Get Account Balance
app.get('/account/me/balance', getAccountNumber, (req, res) => {
    const accountNumber = req.accountNumber;
    
    if (!accounts[accountNumber]) {
        return res.status(404).json({ error: 'Account not found' });
    }

    res.json({
        balance: accounts[accountNumber].balance
    });
});

// Check if Account is Frozen
app.get('/account/me/frozen', getAccountNumber, (req, res) => {
    const accountNumber = req.accountNumber;
    
    if (!accounts[accountNumber]) {
        return res.status(404).json({ error: 'Account not found' });
    }

    res.json({
        frozen: accounts[accountNumber].frozen
    });
});

// Create Transaction (Payment)
app.post('/transaction', getAccountNumber, (req, res) => {
    const fromAccount = req.accountNumber;
    const { to_account_number, to_bank_name, amount, description } = req.body;

    if (!to_account_number || !amount || amount <= 0) {
        return res.status(400).json({ 
            success: false,
            error: 'Invalid transaction parameters' 
        });
    }

    if (!accounts[fromAccount]) {
        return res.status(404).json({ 
            success: false,
            error: 'From account not found' 
        });
    }

    // Check sufficient balance
    if (accounts[fromAccount].balance < amount) {
        console.log("Transaction failed due balance issues")
        return res.status(400).json({ 
            success: false,
            error: 'Insufficient funds' 
        });
    }

    const transactionNumber = generateTransactionNumber();
    const timestamp = getCurrentUnixTimestamp();

    // Deduct from sender
    accounts[fromAccount].balance -= amount;

    // Create transaction record
    transactions[transactionNumber] = {
        transaction_number: transactionNumber,
        from: fromAccount,
        to: to_account_number,
        amount: amount,
        description: description,
        status: 'success',
        timestamp: timestamp
    };

    console.log(`Transaction ${transactionNumber}: ${fromAccount} -> ${to_account_number}, Amount: ${amount}, Description: ${description}`);
    console.log(`Amount transferred: ${amount}`)
    console.log(`New Balance: ${accounts[fromAccount].balance}`)

    // Send notification to recipient if they have notification URL
    if (notificationUrls[to_account_number]) {
        sendNotification(to_account_number, transactions[transactionNumber]);
    }

    res.json({
        success: true,
        transaction_number: transactionNumber,
        status: 'success'
    });
});

// Get Transaction Details
app.get('/transaction/:transaction_number', getAccountNumber, (req, res) => {
    const transactionNumber = req.params.transaction_number;
    const transaction = transactions[transactionNumber];

    if (!transaction) {
        return res.status(404).json({ error: 'Transaction not found' });
    }

    res.json(transaction);
});

// Take Out a Loan
app.post('/loan', getAccountNumber, (req, res) => {
    const accountNumber = req.accountNumber;
    const { amount } = req.body;

    if (!amount || amount <= 0) {
        return res.status(400).json({ 
            success: false,
            error: 'Invalid loan amount' 
        });
    }

    if (!accounts[accountNumber]) {
        return res.status(404).json({ 
            success: false,
            error: 'Account not found' 
        });
    }

    const loanNumber = generateLoanNumber();
    const interestRate = 0.05; // 5% interest
    const totalDue = Math.ceil(amount * (1 + interestRate));

    // Add loan to account
    loans[loanNumber] = {
        loan_number: loanNumber,
        account_number: accountNumber,
        initial_amount: amount,
        outstanding_amount: totalDue,
        interest_rate: interestRate,
        started_at: getCurrentUnixTimestamp(),
        write_off: false,
        payments: []
    };

    // Add money to account balance
    accounts[accountNumber].balance += amount;

    console.log(`Loan ${loanNumber} created for account ${accountNumber}: ${amount} (due: ${totalDue})`);
    console.log("New balance: "+accounts[accountNumber].balance)
    res.json({
        success: true,
        loan_number: loanNumber
    });
});

// Get Outstanding Loans
app.get('/account/me/loans', getAccountNumber, (req, res) => {
    const accountNumber = req.accountNumber;
    
    const accountLoans = Object.values(loans)
        .filter(loan => loan.account_number === accountNumber && loan.outstanding_amount > 0);

    const totalDue = accountLoans.reduce((sum, loan) => sum + loan.outstanding_amount, 0);

    const loanSummary = accountLoans.map(loan => ({
        loan_number: loan.loan_number,
        due: loan.outstanding_amount
    }));

    res.json({
        total_due: totalDue,
        loans: loanSummary
    });
});

// Repay Loan
app.post('/loan/:loan_number/pay', getAccountNumber, (req, res) => {
    const accountNumber = req.accountNumber;
    const loanNumber = req.params.loan_number;
    const { amount } = req.body;

    if (!amount || amount <= 0) {
        return res.status(400).json({ 
            success: false,
            error: 'Invalid payment amount' 
        });
    }

    const loan = loans[loanNumber];
    if (!loan || loan.account_number !== accountNumber) {
        return res.status(404).json({ 
            success: false,
            error: 'Loan not found' 
        });
    }

    if (!accounts[accountNumber]) {
        return res.status(404).json({ 
            success: false,
            error: 'Account not found' 
        });
    }

    if (accounts[accountNumber].balance < amount) {
        return res.status(400).json({ 
            success: false,
            error: 'Insufficient funds' 
        });
    }

    // Deduct from account
    accounts[accountNumber].balance -= amount;

    // Apply payment to loan
    const actualPayment = Math.min(amount, loan.outstanding_amount);
    const overpayment = amount - actualPayment;

    loan.outstanding_amount -= actualPayment;
    
    // Record payment
    loan.payments.push({
        timestamp: getCurrentUnixTimestamp(),
        amount: actualPayment,
        is_interest: false
    });

    // Refund overpayment
    if (overpayment > 0) {
        accounts[accountNumber].balance += overpayment;
    }

    console.log(`Loan payment: ${loanNumber}, paid: ${actualPayment}, overpayment: ${overpayment}, remaining: ${loan.outstanding_amount}`);

    res.json({
        success: true,
        paid: actualPayment,
        overpayment: overpayment
    });
});

// Get Specific Loan Details
app.get('/loan/:loan_number', getAccountNumber, (req, res) => {
    const accountNumber = req.accountNumber;
    const loanNumber = req.params.loan_number;

    const loan = loans[loanNumber];
    if (!loan || loan.account_number !== accountNumber) {
        return res.status(404).json({ error: 'Loan not found' });
    }

    res.json({
        loan_number: loan.loan_number,
        initial_amount: loan.initial_amount,
        outstanding: loan.outstanding_amount,
        interest_rate: loan.interest_rate,
        started_at: loan.started_at,
        write_off: loan.write_off,
        payments: loan.payments
    });
});

// Helper function to send notifications
async function sendNotification(toAccount, transaction) {
    const notificationUrl = notificationUrls[toAccount];
    if (!notificationUrl) return;

    const payload = {
        transaction_number: transaction.transaction_number,
        status: transaction.status,
        amount: transaction.amount,
        timestamp: transaction.timestamp,
        description: transaction.description,
        to: transaction.to,
        from: transaction.from
    };

    try {
        const fetch = await import('node-fetch');
        const response = await fetch.default(notificationUrl, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(payload)
        });

        if (response.ok) {
            console.log(`Notification sent successfully to ${toAccount} at ${notificationUrl}`);
        } else {
            console.log(`Failed to send notification to ${toAccount}: ${response.status}`);
        }
    } catch (error) {
        console.error(`Error sending notification to ${toAccount}:`, error.message);
    }
}

// Debug endpoints
app.get('/debug/accounts', (req, res) => {
    res.json(accounts);
});

app.get('/debug/transactions', (req, res) => {
    res.json(transactions);
});

app.get('/debug/loans', (req, res) => {
    res.json(loans);
});

app.get('/debug/notifications', (req, res) => {
    res.json(notificationUrls);
});

// Reset all data
app.post('/debug/reset', (req, res) => {
    accounts = {};
    transactions = {};
    loans = {};
    notificationUrls = {};
    defaultAccount = null;
    accountCounter = 1000;
    transactionCounter = 1;
    loanCounter = 1;
    
    console.log('All data reset');
    res.json({ message: 'All data reset successfully' });
});

app.listen(port, () => {
   console.log("Commercial Bank Started: Port 3000")
});