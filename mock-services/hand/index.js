const express = require('express');
const app = express();
const port = 3002;

app.use(express.json());

let orders = {};
let orderCounter = 1000;

function generateOrderId() {
    return orderCounter++;
}

function getCurrentUnixTimestamp() {
    return Math.floor(Date.now() / 1000);
}


// Get machines for sale
app.get('/simulation/machines', (req, res) => {
    const machines = [
        {
            machineName: "screen_machine",
            quantity: 10,
            materialRatio: "2:3", // sand:copper ratio
            productionRate: 200, // screens per day
            price: 8500
        }
    ];

    res.json({
        machines: machines
    });
});

// Get raw materials for sale
app.get('/simulation/raw-materials', (req, res) => {
    const materials = [
        {
            rawMaterialName: "sand",
            pricePerKg: 45.0,
            quantityAvailable: 10000
        },
        {
            rawMaterialName: "copper",
            pricePerKg: 85.0,
            quantityAvailable: 8000
        }
    ];

    res.json(materials);
});

// Purchase machine
app.post('/simulation/purchase-machine', (req, res) => {
    try {
        const { machineName, quantity } = req.body;

        if (!machineName || !quantity || quantity <= 0) {
            return res.status(400).json({ 
                error: 'Invalid machine purchase request. MachineName and positive Quantity required.' 
            });
        }

        // Find the machine
        const availableMachines = [
            {
                machineName: "screen_machine",
                quantity: 10,
                materialRatio: "2:3",
                productionRate: 200,
                pricePerMachine: 8500.0,
                weightPerMachine: 250.0
            }
        ];

        const machine = availableMachines.find(m => m.machineName === machineName);

        if (!machine) {
            return res.status(404).json({ error: 'Machine not found' });
        }

        if (machine.quantity < quantity) {
            return res.status(400).json({ 
                error: `Insufficient machine quantity. Available: ${machine.quantity}, Requested: ${quantity}` 
            });
        }

        const orderId = generateOrderId();
        const totalPrice = machine.pricePerMachine * quantity;
        const totalWeight = machine.weightPerMachine * quantity;

        // Store order
        orders[orderId] = {
            orderId: orderId,
            type: 'machine',
            machineName: machineName,
            quantity: quantity,
            unitPrice: machine.pricePerMachine,
            totalPrice: totalPrice,
            weight: totalWeight,
            bankAccount: "HAND_MACHINE_ACC001",
            createdAt: getCurrentUnixTimestamp(),
            status: 'pending'
        };

        // Update available quantity
        machine.quantity -= quantity;

        console.log(`Machine order ${orderId}: ${quantity}x ${machineName} for ${totalPrice}`);

        const response = {
            orderId: orderId,
            machineName: machineName,
            quantity: quantity,
            price: totalPrice,
            weight: totalWeight,
            machineDetails: {
                requiredMaterials: "sand,copper",
                materialRatio: machine.materialRatio,
                productionRate: machine.productionRate
            },
            bankAccount: "HAND_MACHINE_ACC001"
        };

        res.json(response);

    } catch (error) {
        console.error('Error processing machine purchase:', error);
        res.status(500).json({ error: 'Internal server error' });
    }
});

// Purchase raw material
app.post('/simulation/purchase-raw-material', (req, res) => {
    try {
        const { materialName, weightQuantity } = req.body;

        if (!materialName || !weightQuantity || weightQuantity <= 0) {
            return res.status(400).json({ 
                error: 'Invalid material purchase request. MaterialName and positive WeightQuantity required.' 
            });
        }

        // Available materials with current pricing
        const availableMaterials = [
            {
                name: "sand",
                pricePerKg: 45.0,
                quantityAvailable: 10000,
                bankAccount: "HAND_SAND_ACC001"
            },
            {
                name: "copper", 
                pricePerKg: 85.0,
                quantityAvailable: 8000,
                bankAccount: "HAND_COPPER_ACC001"
            }
        ];

        const material = availableMaterials.find(m => 
            m.name.toLowerCase() === materialName.toLowerCase());

        if (!material) {
            return res.status(404).json({ error: 'Material not found' });
        }

        if (material.quantityAvailable < weightQuantity) {
            return res.status(400).json({ 
                error: `Insufficient material quantity. Available: ${material.quantityAvailable}kg, Requested: ${weightQuantity}kg` 
            });
        }

        const orderId = generateOrderId();
        const totalPrice = material.pricePerKg * weightQuantity;

        // Store order
        orders[orderId] = {
            orderId: orderId,
            type: 'material',
            materialName: materialName,
            weightQuantity: weightQuantity,
            pricePerKg: material.pricePerKg,
            totalPrice: totalPrice,
            bankAccount: material.bankAccount,
            createdAt: getCurrentUnixTimestamp(),
            status: 'pending'
        };

        // Update available quantity
        material.quantityAvailable -= weightQuantity;

        console.log(`Material order ${orderId}: ${weightQuantity}kg ${materialName} for ${totalPrice}`);

        const response = {
            orderId: orderId,
            materialName: materialName,
            weightQuantity: weightQuantity,
            price: totalPrice,
            bankAccount: material.bankAccount
        };

        res.json(response);

    } catch (error) {
        console.error('Error processing material purchase:', error);
        res.status(500).json({ error: 'Internal server error' });
    }
});

// Get order details (optional endpoint for debugging)
app.get('/simulation/order/:orderId', (req, res) => {
    const orderId = parseInt(req.params.orderId);
    const order = orders[orderId];

    if (!order) {
        return res.status(404).json({ error: 'Order not found' });
    }

    res.json(order);
});

// Debug endpoints
app.get('/debug/orders', (req, res) => {
    res.json(orders);
});

app.listen(port, () => {
    console.log(`Hand of Hḗphaistos Mock Service started on port ${port}`);
});