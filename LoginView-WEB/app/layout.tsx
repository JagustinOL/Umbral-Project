import type { Metadata } from 'next'
import './globals.css'

export const metadata: Metadata = {
  title: 'UMBRAL — Sign In',
  description: 'Sign in to UMBRAL with your Keycloak account.',
  icons: {
    icon: '/icon.svg',
  },
}

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode
}>) {
  return (
    <html lang="en" className="bg-background">
      <body className="font-sans antialiased min-h-screen">
        {children}
      </body>
    </html>
  )
}
