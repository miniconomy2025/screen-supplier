const express = require('express');
const app = express();
const port = 3001;

app.use(express.json());

let shipmentCounter = 1000;

function generateShipmentId() {
    return shipmentCounter++;
}

// Create Pickup Request
app.post('/api/pickup-request', (req, res) => {
    try {
        const { originCompanyId, destinationCompanyId, originalExternalOrderId, items } = req.body;

        // Basic validation
        if (!originCompanyId || !destinationCompanyId || !originalExternalOrderId || !items || !Array.isArray(items)) {
            return res.status(400).json({ 
                error: 'Missing required fields: originCompanyId, destinationCompanyId, originalExternalOrderId, items' 
            });
        }

        const shipmentId = generateShipmentId();
        const bankAccountNumber = "BULK_LOGISTICS_ACC001";
        const price = 150; // Fixed price for simplicity

        console.log(`Pickup request created for shipment ID: ${shipmentId}`);

        res.json({
            success: true,
            pickupRequestId: shipmentId.toString(),
            bulkLogisticsBankAccountNumber: bankAccountNumber,
            cost: price
        });

    } catch (error) {
        res.status(500).json({ error: 'Internal server error' });
    }
});

app.listen(port, () => {
    console.log(`Bulk Logistics Mock Service started on port ${port}`);
});