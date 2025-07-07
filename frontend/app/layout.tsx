import type React from "react"
import type { Metadata } from "next"

export const metadata: Metadata = {
  title: "Manufacturing Dashboard",
  description: "Screen manufacturing reporting dashboard",
    generator: 'v0.dev'
}

export default function RootLayout({
  children,
}: {
  children: React.ReactNode
}) {
  return (
    <html lang="en">
      <body>{children}</body>
    </html>
  )
}
