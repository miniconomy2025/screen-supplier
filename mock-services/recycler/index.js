const express = require('express');
const app = express();
const port = 3007;

app.use(express.json());

let orders = {};
let orderCounter = 1000;

function generateOrderId() {
    return orderCounter++;
}

function getCurrentUnixTimestamp() {
    return Math.floor(Date.now() / 1000);
}

// Get raw materials for sale
app.get('/materials', (req, res) => {
    const materials = [
        {
            id: 1,
            name: "sand",
            price: 45.0,
            available_quantity_in_kg: 10000
        },
        {
            id: 2,
            name: "copper",
            price: 85.0,
            available_quantity_in_kg: 8000
        }
    ];

    res.json(materials);
});

// Purchase raw material
app.post('/orders', (req, res) => {
    try {
        const { companyName, items } = req.body;

        console.log(`Request to buy from ${companyName}`)

        const materialName = items[0].rawMaterialName;
        const weightQuantity = items[0].quantityInKg;

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
            // orderId: orderId,
            // materialName: materialName,
            // weightQuantity: weightQuantity,
            // price: totalPrice,
            // bankAccount: material.bankAccount
            data: {
                orderId: orderId,
                accountNumber: material.bankAccount,
                OrderItems: [
                    {
                        quantityInKg: weightQuantity,
                        pricePerKg: material.pricePerKg
                    }
                ]
            }
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
    console.log(`Recycler Mock Service started on port ${port}`);
});