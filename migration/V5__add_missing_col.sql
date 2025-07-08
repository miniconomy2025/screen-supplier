ALTER TABLE equipment_parameters
ADD COLUMN equipment_weight integer NOT NULL DEFAULT 0;

ALTER TABLE screen_orders
ADD COLUMN quantity_collected integer NOT NULL DEFAULT 0;