# Frontend CNAME
resource "azurerm_dns_cname_record" "screen_supplier_frontend" {
  name                = "screen-supplier"
  record              = "dpoavifu2v2b1.cloudfront.net"
   
  zone_name           = data.azurerm_dns_zone.grad_projects_dns_zone.name
  resource_group_name = "the-hive"
  ttl                 = 3600
  tags                = local.common_tags
}

# API CNAME  
resource "azurerm_dns_cname_record" "screen_supplier_api" {
  name                = "screen-supplier-api"
  record              = "d2x6yfq7c2o9mn.cloudfront.net"
   
  zone_name           = data.azurerm_dns_zone.grad_projects_dns_zone.name
  resource_group_name = "the-hive"
  ttl                 = 3600
  tags                = local.common_tags
}

# SSL validation for frontend domain
resource "azurerm_dns_cname_record" "screen_supplier_ssl_validation_frontend" {
  name                = "_df7af2d290b435945408b002995562d9.screen-supplier"
  record              = "_f1f984ff78b07a9622f284b55a684649.xlfgrmvvlj.acm-validations.aws."
   
  zone_name           = data.azurerm_dns_zone.grad_projects_dns_zone.name
  resource_group_name = "the-hive"
  ttl                 = 300
  tags                = local.common_tags
}

# SSL validation for API domain
resource "azurerm_dns_cname_record" "screen_supplier_ssl_validation_api" {
  name                = "_ccfc8bb87e62b54a749e29b55f3264a6.screen-supplier-api"
  record              = "_5622eda184d24e95b3966bd59ed4697f.xlfgrmvvlj.acm-validations.aws."
   
  zone_name           = data.azurerm_dns_zone.grad_projects_dns_zone.name
  resource_group_name = "the-hive"
  ttl                 = 300
  tags                = local.common_tags
}