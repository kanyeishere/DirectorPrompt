-- 移除复合状态类型: 删除 composite_items 表

DROP INDEX IF EXISTS idx_composite_items_session;
DROP INDEX IF EXISTS idx_composite_items_attr;
DROP TABLE IF EXISTS composite_items;
