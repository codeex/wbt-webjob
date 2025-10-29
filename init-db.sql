-- 初始化数据库
CREATE DATABASE IF NOT EXISTS wbt_webjob CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
CREATE DATABASE IF NOT EXISTS wbt_webjob_hangfire CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

-- 创建用户并授予权限
CREATE USER IF NOT EXISTS 'wbt_user'@'%' IDENTIFIED BY 'wbt_password';
GRANT ALL PRIVILEGES ON wbt_webjob.* TO 'wbt_user'@'%';
GRANT ALL PRIVILEGES ON wbt_webjob_hangfire.* TO 'wbt_user'@'%';
FLUSH PRIVILEGES;
