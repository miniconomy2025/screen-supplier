INSERT INTO "order_status" ("name") VALUES 
('waiting_payment'),
('waiting_collection'), 
('collected'),
('waiting_delivery'),
('delivered'),
('abandoned');

CREATE INDEX IF NOT EXISTS idx_screen_orders_status ON "screen_orders"("status_id");
CREATE INDEX IF NOT EXISTS idx_purchase_orders_status ON "purchase_orders"("status_id");